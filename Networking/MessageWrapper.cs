using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AutoFollow.Networking
{
    [DataContract]
    public class MessageWrapper
    {
        [DataMember]
        public Message PrimaryMessage { get; set; }

        [DataMember]
        public List<Message> OtherMessages { get; set; }
    }
}
