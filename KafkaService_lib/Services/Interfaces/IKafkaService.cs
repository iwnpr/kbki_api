using Confluent.Kafka;

namespace KafkaService_lib.Services.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IKafkaService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        bool IsAvailable();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        Task<bool> Produce(List<Message<string, string>> messages, string? topic = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<bool> Produce(Message<Null, string> message, string? topic = null);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Message<string, string>? Consume();
    }
}
