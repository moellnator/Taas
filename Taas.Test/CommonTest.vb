Imports Taas.BackEnd
Imports Taas.Common.Logging
Imports Taas.HostProcess

<TestClass()> Public Class CommonTest

    <TestMethod, TestCategory("Logging")> Public Sub TestLogger()
        Logger.AddDevice(New TraceLogDevice)
        Logger.Verbosity = Level.Debug
        Logger.Information("Logging works.")
    End Sub

    Public Class TraceLogDevice : Implements IDevice

        Public Sub AddEntry(entry As Entry) Implements IDevice.AddEntry
            Trace.WriteLine(entry.TimeStamp.ToLongTimeString & "  " & entry.Level.ToString & "  " & entry.Message)
        End Sub

    End Class

End Class
