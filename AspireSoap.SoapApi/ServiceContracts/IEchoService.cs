﻿using CoreWCF;

namespace AspireSoap.SoapApi.ServiceContracts;

[ServiceContract(Name = "IEchoService")]
public interface IEchoService
{
    [OperationContract]
    Task<string> Echo(string text);

    [OperationContract]
    string ComplexEcho(EchoMessage text);

    [OperationContract]
    [FaultContract(typeof(EchoFault))]
    string FailEcho(string text);

    [OperationContract]
    [FaultContract(typeof(EchoFault))]
    PingOutput Ping();
}

