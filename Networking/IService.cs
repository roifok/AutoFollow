using System.ServiceModel;

namespace AutoFollow.Networking
{
    [ServiceContract]
    interface IService
    {
        [OperationContract]
        MessageWrapper GetMessageFromServer();

        [OperationContract]
        void SendMessageToServer(MessageWrapper message);
    }
}
