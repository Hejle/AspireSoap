using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace AspireSoap.SoapApi.ServiceContracts;

[DataContract]

public class EchoMessage
{
    [AllowNull]
    [DataMember]
    public string Text { get; set; }
}