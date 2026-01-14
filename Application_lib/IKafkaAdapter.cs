using Confluent.Kafka;

namespace Application_lib
{
    /// <summary>
    /// 
    /// </summary>
    public interface IKafkaAdapter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<bool> Produce(Message<Null, string> message, string? topic = null);
    }
}
