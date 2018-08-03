Namespace Logging

    Public Enum Level
        Critical
        Exception
        Warning
        Information
        Debug
    End Enum

    Public Class Logger

        Private Shared ReadOnly _queue_mutex As New Threading.Mutex
        Private Shared ReadOnly _message_queue As New List(Of Entry)
        Private Shared ReadOnly _devices As New List(Of Device)
        Private Shared ReadOnly _runtime As New Threading.Thread(AddressOf _loop)
        Private Shared _is_exit As Boolean = False

        Public Shared Sub AddDevice(device As Device)
            If Not _devices.Contains(device) Then _devices.Add(device)
        End Sub

        Private Shared Sub _exit()
            _is_exit = True
        End Sub

        Shared Sub New()
            _runtime.Start()
            AddHandler AppDomain.CurrentDomain.ProcessExit, AddressOf _exit
        End Sub

        Public Shared Property Verbosity As Level = Level.Warning

        Public Shared Sub Critical(message As String)
            _queue_message(Level.Critical, message)
        End Sub

        Public Shared Sub Exception(message As String)
            _queue_message(Level.Exception, message)
        End Sub

        Public Shared Sub Warning(message As String)
            _queue_message(Level.Warning, message)
        End Sub

        Public Shared Sub Information(message As String)
            _queue_message(Level.Information, message)
        End Sub

        Public Shared Sub Debug(message As String)
            _queue_message(Level.Debug, message)
        End Sub

        Private Shared Sub _loop()
            Try
                While Not _is_exit
                    If _message_queue.Count > 0 Then
                        _queue_mutex.WaitOne()
                        Dim entry As Entry = _message_queue.OrderBy(Function(e) e.TimeStamp).First
                        _message_queue.Remove(entry)
                        _queue_mutex.ReleaseMutex()
                        For Each d As Device In _devices
                            d.AddEntry(entry)
                        Next
                    Else
                        Threading.Thread.Sleep(1)
                    End If
                End While
            Catch thex As Threading.ThreadAbortException
                AwaitQueueDrained()
            End Try
        End Sub

        Public Shared Sub AwaitQueueDrained()
            _queue_mutex.WaitOne()
            For Each ent As Entry In _message_queue.OrderBy(Function(e) e.TimeStamp)
                For Each d As Device In _devices
                    d.AddEntry(ent)
                Next
            Next
            _queue_mutex.ReleaseMutex()
        End Sub

        Private Shared Sub _queue_message(level As Level, message As String)
            If level <= Verbosity Then
                _queue_mutex.WaitOne()
                _message_queue.Add(New Entry(level, message))
                _queue_mutex.ReleaseMutex()
            End If
        End Sub

    End Class

End Namespace
