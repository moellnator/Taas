
Imports Taas.Common
Imports Taas.Common.IPC
Imports Taas.Common.Logging

Public Class TaskClient

    Private ReadOnly _pipe_loop As New Threading.Thread(AddressOf Me._loop_internal)
    Private WithEvents _pipe_handler As PipeHandler
    Private ReadOnly _mre As New Threading.ManualResetEvent(False)
    Private ReadOnly _tb As New TaskBuilder

    Public Shared Function Connect(pipeName As String) As TaskClient
        Logger.AddDevice(ConsoleDevice.Instance)
        Logger.Verbosity = Level.Debug
        Return New TaskClient(pipeName)
    End Function

    Private Sub New(pipeName As String)
        Me._pipe_handler = New PipeHandler(
            New IO.Pipes.NamedPipeClientStream(".", pipeName & ".out", IO.Pipes.PipeDirection.In, IO.Pipes.PipeOptions.None),
            New IO.Pipes.NamedPipeClientStream(".", pipeName & ".in", IO.Pipes.PipeDirection.Out, IO.Pipes.PipeOptions.None)
        )
        Logger.Debug("Initializing IPC pipe @" & pipeName)
    End Sub

    Public Sub Run()
        Me._pipe_loop.Start()
        Me._mre.WaitOne()
        Try
            Logger.Information("Start task execution.")
            Me._tb.Execute()
            Logger.Information("End task execution.")
        Catch ex As Exception
            Me._handle_exception(ex)
        End Try
        Me._pipe_loop.Abort()
        Me._pipe_handler.PipeIn.Close()
        Me._pipe_handler.PipeOut.Close()
        Logger.Information("Application is closing.")
        End
    End Sub

    Private Sub _loop_internal()
        Try
            DirectCast(Me._pipe_handler.PipeOut, IO.Pipes.NamedPipeClientStream).Connect()
            DirectCast(Me._pipe_handler.PipeIn, IO.Pipes.NamedPipeClientStream).Connect()
            If Not Me._pipe_handler.PipeIn.ReadByte() = Protocol.HandShake Then _
                Throw New Exception("Handshake with task server failed during initialization.")
            Me._pipe_handler.PipeOut.WriteByte(Protocol.HandShake)
            Me._pipe_handler.PipeOut.Flush()
            Logger.Information("Initialized IPC communication.")
            Me._pipe_handler.Run()
        Catch thex As Threading.ThreadAbortException
        Catch ex As Exception
            Me._handle_exception(ex)
        End Try
    End Sub

    Private Sub _pipe_handler_PipeData(sender As Object, e As PipeDataEventArgs) Handles _pipe_handler.PipeData
        Try
            Logger.Debug("Received data: " & e.Data.Count & " Bytes")
            Select Case e.Data.First
                Case Protocol.Execute
                    Me._mre.Set()
                Case Protocol.Library
                    Me._tb.SetLibrary(Protocol.GetStringArgument(e.Data))
                    Logger.Information("Assembly loaded " & Me._tb.Assembly.FullName)
                Case Protocol.ClassInfo
                    Me._tb.BuildClass(Protocol.GetStringArgument(e.Data))
                    Logger.Information("Instance created from " & Me._tb.InstanceType.FullName)
                Case Else
                    Throw New ArgumentException("Invalid character in command stream.")
            End Select
        Catch ex As Exception
            Me._handle_exception(ex)
        End Try
    End Sub

    Private Sub _handle_exception(ex As Exception)
        Try
            Logger.Exception(ex.Message & vbNewLine & ex.StackTrace)
            Dim exdata As String = "[" & ex.GetType.Name & "] " & ex.Message & "; " & ex.StackTrace
            Me._pipe_handler.SendMessageSafe(Protocol.BuildStringCommand(Protocol.Critial, exdata))
            Me._pipe_loop.Abort()
        Catch
        Finally
            End
        End Try
    End Sub

End Class
