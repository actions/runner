using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Services.Commerce
{
    public class ConnectedServer
    {
        /// <summary>
        /// The id of the subscription used for purchase
        /// </summary>
        public Guid SubscriptionId { get; set; }

        /// <summary>
        /// Hosted AccountName associated with the connected server
        /// NOTE: As of S112, this is now the collection name. Not changed as this is exposed to client code.
        /// </summary>
        public String AccountName { get; set; }

        /// <summary>
        /// Hosted AccountId associated with the connected server
        /// NOTE: As of S112, this is now the CollectionId. Not changed as this is exposed to client code.
        /// </summary>
        public Guid AccountId { get; set; }

        /// <summary>
        /// OnPrem server id associated with the connected server
        /// </summary>
        public Guid ServerId { get; set; }

        /// <summary>
        /// OnPrem server associated with the connected server
        /// </summary>
        public String ServerName { get; set; }

        /// <summary>
        /// OnPrem target host associated with the connected server.  Typically the
        /// collection host id
        /// </summary>
        public Guid TargetId { get; set; }

        /// <summary>
        /// OnPrem target associated with the connected server.  
        /// </summary>
        public String TargetName { get; set; }

        /// <summary>
        /// SpsUrl of the hosted account that the onrepm server has been connected to.
        /// </summary>
        public String SpsUrl { get; set; }

        /// <summary>
        /// Object used to create credentials to call from OnPrem to hosted service.
        /// </summary>
        public ConnectedServerAuthorization Authorization { get; set; }
    }
}
