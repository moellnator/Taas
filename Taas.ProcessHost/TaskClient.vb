Imports Taas.BackEnd

Public Class TaskClient

    Private ReadOnly _pipe_loop As New Threading.Thread(AddressOf Me._loop_internal)
    Private WithEvents _pipe_handler As TaskPipe
    Private ReadOnly _mre As New Threading.ManualResetEvent(False)
    Private ReadOnly _tb As New TaskBuilder

    Public Shared Function Connect(pipeName As String) As TaskClient
        Return New TaskClient(pipeName)
    End Function

    Private Sub New(pipeName As String)
        Me._pipe_handler = New TaskPipe(
            New IO.Pipes.NamedPipeClientStream(".", pipeName & ".out", IO.Pipes.PipeDirection.In, IO.Pipes.PipeOptions.None),
            New IO.Pipes.NamedPipeClientStream(".", pipeName & ".in", IO.Pipes.PipeDirection.Out, IO.Pipes.PipeOptions.None)
        )
    End Sub

    Public Sub Run()
        Me._pipe_loop.Start()
        Me._mre.WaitOne()
        Try
            Me._tb.Execute()
            'TODO: Send 'finished' to server
        Catch ex As Exception
            'TODO: Send exception to server
            Console.WriteLine("Exception: " & ex.Message)
        End Try
        Me._pipe_loop.Abort()
        Me._pipe_handler.PipeIn.Close()
        Me._pipe_handler.PipeOut.Close()
    End Sub

    Private Sub _loop_internal()
        Try
            DirectCast(Me._pipe_handler.PipeOut, IO.Pipes.NamedPipeClientStream).Connect()
            DirectCast(Me._pipe_handler.PipeIn, IO.Pipes.NamedPipeClientStream).Connect()
            If Not Me._pipe_handler.PipeIn.ReadByte() = TaskPipeProtocol.HandShake Then
                'TODO: Try to send an exception and exit the application.
            End If
            Me._pipe_handler.PipeOut.WriteByte(TaskPipeProtocol.HandShake)
            Me._pipe_handler.PipeOut.Flush()
            Me._pipe_handler.Run()
        Catch thex As Threading.ThreadAbortException
        Catch ex As Exception
            'TODO: Send exception to server
            Console.WriteLine("Exception: " & ex.Message)
        End Try
    End Sub

    Private Sub _pipe_handler_PipeData(sender As Object, e As TaskPipeDataEventArgs) Handles _pipe_handler.PipeData
        Try
            Select Case e.Data.First
                Case TaskPipeProtocol.Execute
                    Me._mre.Set()
                Case TaskPipeProtocol.Library
                    Me._tb.SetLibrary(TaskPipeProtocol.GetStringArgument(e.Data))
                Case TaskPipeProtocol.ClassInfo
                    Me._tb.BuildClass(TaskPipeProtocol.GetStringArgument(e.Data))
                Case Else
                    Throw New ArgumentException("Invalid character in command stream.")
            End Select
        Catch ex As Exception
            'TODO: Send exception to server
            Console.WriteLine("Exception: " & ex.Message)
        End Try
    End Sub

End Class
