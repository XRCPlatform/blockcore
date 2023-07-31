using Blockcore.Controllers.Converters;
using Newtonsoft.Json;

namespace Blockcore.Features.RPC.Models
{
//    {                                              (json object)
//  "totalbytesrecv" : n,                        (numeric) Total bytes received
//  "totalbytessent" : n,                        (numeric) Total bytes sent
//  "timemillis" : xxx,                          (numeric) Current UNIX epoch time in milliseconds
//  "uploadtarget" : {                           (json object)
//    "timeframe" : n,                           (numeric) Length of the measuring timeframe in seconds
//    "target" : n,                              (numeric) Target in bytes
//    "target_reached" : true|false,             (boolean) True if target is reached
//    "serve_historical_blocks" : true|false,    (boolean) True if serving historical blocks
//    "bytes_left_in_cycle" : n,                 (numeric) Bytes left in current time cycle
//    "time_left_in_cycle" : n(numeric) Seconds left in current time cycle
//}
//}

    public class GetNetTotalsModel
    {

        [JsonProperty(Order = 0, PropertyName = "totalbytesrecv")]
        public long TotalBytesRecv { get; set; }

        [JsonProperty(Order = 1, PropertyName = "totalbytessent")]
        public long TotalBytesSent { get; set; }

        [JsonProperty(Order = 2, PropertyName = "timemillis")]
        public long TimeMillis { get; set; }
    }
}
