Namespace API
    Public Class WinAPIProvider : Implements IProvider
        <Flags()> Public Enum ThreadAccess As Integer
            TERMINATE = 1
            SUSPEND_RESUME = 2
            GET_CONTEXT = 8
            SET_CONTEXT = 16
            SET_INFORMATION = 32
            QUERY_INFORMATION = 64
            SET_THREAD_TOKEN = 128
            IMPERSONATE = 256
            DIRECT_IMPERSONATION = 512
        End Enum

        Private Declare Function OpenThread Lib "kernel32.dll" (ByVal dwDesiredAccess As ThreadAccess, ByVal bInheritHandle As Boolean, ByVal dwThreadId As UInteger) As IntPtr
        Private Declare Function SuspendThread Lib "kernel32.dll" (ByVal hThread As IntPtr) As UInteger
        Private Declare Function ResumeThread Lib "kernel32.dll" (ByVal hThread As IntPtr) As Integer
        Private Declare Function CloseHandle Lib "kernel32" (ByVal handle As IntPtr) As Boolean

        Private Shared _instance As WinAPIProvider
        Public Shared Function GetInstance() As WinAPIProvider
            If _instance Is Nothing Then _instance = New WinAPIProvider
            Return _instance
        End Function

        Private Sub New()
        End Sub

        Public Sub SuspendProcess(id As Integer) Implements IProvider.SuspendProcess
            _suspend_Process(id)
        End Sub

        Public Sub ResumeProcess(id As Integer) Implements IProvider.ResumeProcess
            _resume_process(id)
        End Sub

        Private Shared Sub _suspend_Process(id As Integer)
            Dim process As Process = Process.GetProcessById(id)
            If (process.ProcessName = String.Empty) Then _
                Throw New Exception("Process (" & id & ") not found.")
            For Each pT As ProcessThread In process.Threads
                Dim pOpenThread As IntPtr = OpenThread(ThreadAccess.SUSPEND_RESUME, False, pT.Id)
                If (pOpenThread = IntPtr.Zero) Then Continue For
                SuspendThread(pOpenThread)
                CloseHandle(pOpenThread)
            Next
        End Sub

        Private Shared Sub _resume_process(id As Integer)
            Dim process As Process = Process.GetProcessById(id)
            If (process.ProcessName = String.Empty) Then _
                Throw New Exception("Process (" & id & ") not found.")
            For Each pT As ProcessThread In process.Threads
                Dim pOpenThread As IntPtr = OpenThread(ThreadAccess.SUSPEND_RESUME, False, pT.Id)
                If (pOpenThread = IntPtr.Zero) Then Continue For
                Dim suspendCount = 0
                Do Until (suspendCount > 0)
                    suspendCount = ResumeThread(pOpenThread)
                Loop
                CloseHandle(pOpenThread)
            Next
        End Sub

    End Class

End Namespace
