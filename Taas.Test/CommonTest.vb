Imports Taas.BackEnd
Imports Taas.Common.Logging
Imports Taas.HostProcess

<TestClass()> Public Class CommonTest

    <TestMethod, TestCategory("Logging")> Public Sub TestLogger()
        Logger.AddDevice(TraceLogDevice.GetInstance)
        Logger.Verbosity = Level.Debug
        Logger.Information("Logging works.")
    End Sub

    Public Class TraceLogDevice : Inherits Device

        Private Shared _instance As TraceLogDevice
        Public Shared Function GetInstance() As TraceLogDevice
            If _instance Is Nothing Then _instance = New TraceLogDevice
            Return _instance
        End Function
        Private Sub New()
        End Sub

        Public Overrides Sub AddEntry(entry As Entry)
            Trace.WriteLine(entry.TimeStamp.ToLongTimeString & "  " & entry.Level.ToString & "  " & entry.Message)
        End Sub

    End Class

End Class
