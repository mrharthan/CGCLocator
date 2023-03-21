using System.Collections.Generic;
using System.Text;
using ESRI.ArcGIS.SOESupport;

namespace LRSLocator
{
    /// <summary>
    /// Json Helper Methods
    /// </summary>
    public static class JSONHelper
    {
        public static JsonObject BuildErrorObject(int code, string message, List<string> details = null)
        {
            JsonObject errorObj = new JsonObject();
            errorObj.AddLong("code", code);
            errorObj.AddString("message", message);
            if (details == null)
            {
                errorObj.AddArray("details", new object[0]);
            }
            else
            {
                errorObj.AddArray("details", details.ToArray());
            }

            JsonObject outer = new JsonObject();
            outer.AddJsonObject("error", errorObj);
            return outer;
        }

        public static byte[] BuildErrorObjectAsBytes(int code, string message, List<string> details = null)
        {
            return EncodeResponse(BuildErrorObject(code, message, details));
        }

        public static byte[] EncodeResponse(object response)
        {
            // Handle various output data types
            string strRetval = null;
            if (response is byte[])
            {
                return (byte[])response;
            }
            else if (response is JsonObject)
            {
                strRetval = ((JsonObject)response).ToJson();
            }
            else if (response != null)
            {
                strRetval = response.ToString();
            }
            else
            {
                strRetval = "{}";
            }
            return Encoding.UTF8.GetBytes(strRetval);
        }


    }
}
