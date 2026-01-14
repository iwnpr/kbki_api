using System.Net;

namespace Domain.QBCHServiceModels
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class BaseRedisMessage
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public abstract BaseRedisMessage SetResponseCode(HttpStatusCode? code);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="signedResponseData"></param>
        /// <returns></returns>
        public abstract BaseRedisMessage SetSignedResponse(byte[] signedResponseData);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unsignedResponseXML"></param>
        /// <returns></returns>
        public abstract BaseRedisMessage SetResponseXml(byte[] unsignedResponseXML);

    }
}
