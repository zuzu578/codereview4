//App 에서 QR code 를 스캔할때

var g_ScanQRCode_callback;
function ScanQRCode(callback_name){
	var param = 'cmd = ScanQRCode';
	param += '&cmd_type=NATIVE';
	g_ScanQRCode_callback = callback_name;
	NM.ExecuteCommand(param);
}
// QR code 가 undefined 인 경우
function ScanQRCodeCallback(qrcode){
	if(g_ScanQRCode_callback == undefined)
		return;
	window[g_ScanQRCode_callback](qrcode);

}

//스캔한 QR parameter 를  Handler 에 전달 
function ScanQRCodeCallback_SetScreenlock(qrcode){

	var param = 'cmd=SetScreenlockQR';
	param += '&qrcode ='+encodeURIComponent(qrcode);
	param += '&cmd_type = HTTP';
	param += '&callback = SetScreenlockQRCallback';
	NM.ExecuteCommand(param);
}


// Handler 부분 

case "SetScreenlockQR":
{
	param.Add("screenlock_QR_no",Request.Params["qrcode"]);
	param.Add("user_no",Request.Params["user_no"]);
	param.Add("company_no",Request.Params["company_no"]);
	Response.Write(new JavaScriptSerializer().Serialize(SetScreenlockQR(param)));
	Response.End();

}
break;



// QR Parameter => DataBase 
public Dictionary<string , object> SetScreenlockQR(Dictionary<string , object> param)
{

	Dictionary<string , object> result = null; 
	
	try
	{
	using (DataTable dt = dac.SetScreenlockQR(param))
	{
		if(dt == null || dt.Rows.Count <= 0)
		{
			result = new Dictionary<string , object>();
			result.Add("result", "failure");
			result.Add("code","7");
			return result;
			result = new Dictionary<string , object>();
			result.Add("result","success");
			result.Add("code","0");
			result.Add("screenlock_QR_no",dt.Rows[0]["screenlock_QR_no"]);
			return result; 
		}
		catch(Exception e)
		{
			BasePage basePage = new BasePage();
			return basePage.GetErrorMessage(e, param); 

		}	
}



}


// DataBase 
public DataTable SetScreenlockQR(Dictionary<string ,object> param)

{
	//Procedure Name 
	string spName = "sp_screenlock_QR_insert";
	string connectionString = ConfigurationManager.ConnectionStrings["ConnDB"].ConnectionString;
	List<SqlParameter> sqlParam = new List<SqlParameter> () ; 
	//Parameter( ) 
	sqlParam.Add(new SqlParameter("@screenlock_QR_no", m_dbHelper.GetValueFromParam(param,"screenlock_QR_no)));
	sqlParam.Add(new SqlParameter("@user_no", m_dbHelper.GetValueFromParam(param,"user_no")));
	sqlParam.Add(new SqlParameter("@screenlock_approve_YN",m_dbHelper.GEtValueFromParam(param,"screenlock_approve_YN")));
	return m_dbHelper.ExecuteSP(spName , connectionString , sqlParam);




}	
//QR approve 작동 원리 
// *원리 : 초기 default 값은 N 이다 => 즉 , QR 이 스캔 되지않았을땐 N 으로 인식 ,
// => 반면 , QR 이 스캔되었을때는 Y 로 인식이 되게 한다 

public Dictionary<string , object> SetScreenLockApprove(Dictionary<stirng , object> param)
{
	Dictionary<stirng , obejct> result = null;
	try
	{
		// 초기 screenlock_approve_YN 의 값을 N 으로 해둔다 => 아직 QR 스캔하기 전 
		param["screenlock_approve_YN"] = "N";
		using (DataTable dt = dac.GetScreenlockQR(param))
	
	}
		//QR 코드가 정상적으로 스캔 되지 않았을 경우
		if(dt == null || dt.Rows.Count <= 0)
		{
			result = new Dictionary<string ,object>();
			result.Add("result","failure");
			result.Add("code","1");
			result.Add("message","모바일앱에서 QR코드를 스캔해주세요.");
			return result;

		}
		param["screenlock_QR_no"] = dt.Rows[0]["screenlock_QR_no"];
		param["screenlock_approve_YN"] = "Y";
		using (DataTable dt2  = dac.SetScreenlockQR(param))
		{
			//DataTable != null 일경우 
			if(dt2 == null || dt2.Rows.Count <= 0)
			{
				result = new Dictionary<string , object> () ; 
				result.Add("result", "failure");
				result.Add("code","3");
				return result;


			}
			result = new Dictionary<string , object>();
			result.Add("result","success");
			result.Add("code","0");
			return result;

		}
  catch (Exception e)
            {
                BasePage basePage = new BasePage();
                return basePage.GetErrorMessage(e, param);
            }


}

//dac.GetScreenlockQR 을 추적해보면 

public DataTable SetScreenlockQR(Dictionary<string , object> param)
{
	// param ==> @screenlock_approve_YN
	string spName = "sp_screenlock_QR_select";
            string connectionString = ConfigurationManager.ConnectionStrings["ConnDB"].ConnectionString;
            List<SqlParameter> sqlParam = new List<SqlParameter>();
	// 여기에선 parameter 로 전달해준 @screenlock_approve_YN 과 , user_no 를 추가해주어서 어떤 유저의 QR 을 승인할지를 전달해주는 param

            sqlParam.Add(new SqlParameter("@user_no", m_dbHelper.GetValueFromParam(param, "user_no")));
            sqlParam.Add(new SqlParameter("@screenlock_approve_YN", m_dbHelper.GetValueFromParam(param, "screenlock_approve_YN")));
            return m_dbHelper.ExecuteSP(spName, connectionString, sqlParam);





}
//procedure 
CREATE PROCEDURE [dbo].[sp_screenlock_QR_select]	
	-- @user_no , @screenlock_approve_YN 이 parameter 로 전달되는 상황 	
	@user_no					INT,
	@screenlock_approve_YN		CHAR(1)	

AS
BEGIN	
	SET NOCOUNT ON;

	SELECT TOP 1 SQ.screenlock_QR_no, SQ.screenlock_approve_YN, SQ.screenlock_approve_time, SQ.createdTime
	FROM [dbo].[tb_screenlock_QR] SQ WITH(NOLOCK)
	WHERE SQ.user_no = @user_no
	AND SQ.screenlock_approve_YN = CASE WHEN @screenlock_approve_YN IS NULL THEN SQ.screenlock_approve_YN ELSE @screenlock_approve_YN END
	ORDER BY createdTime DESC
END








