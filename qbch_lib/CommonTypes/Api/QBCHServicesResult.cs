using Common_lib.Models.ServiceModels;

namespace QBCH_lib.CommonTypes.Api
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class QBCHServicesResult<T> : Result<T> where T : class
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static QBCHServicesResult<T> Success(T data)
        {
            return new()
            {
                IsSuccess = true,
                Data = data
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static QBCHServicesResult<T> Error(int code, string? message, T? data = null)
        {
            return new()
            {
                IsSuccess = false,
                Data = data,
                InnerError = new()
                {
                    ErrorCode = code,
                    Message = message
                }
            };
        }

    }
}
