Imports System.IO.Pipes
Imports Taas.BackEnd
Imports Taas.Common
Imports Taas.Common.IPC
Imports Taas.Common.Logging

Public Enum TaskState
    Invalid = -1
    Initializing
    Initialized
    Disposed
    Executing
    Finished
    Failed
    Paused
    Aborting
    Aborted
End Enum

Public Class TaskServer : Implements IDisposable, IEquatable(Of TaskServer)

    Public Event StateChange As TaskEventHandler

    Public ReadOnly Property ID As Integer

    Public ReadOnly Property State As TaskState
        Get
            Return Me._current_state
        End Get
    End Property

    Public ReadOnly Property LastException As Exception
        Get
            Return Me._exception
        End Get
    End Property

    Private _is_disposed As Boolean
    Private _engine As TaskEngine
    Private _current_state As TaskState = TaskState.Invalid
    Private _options As TaskOptions = Nothing
    Private WithEvents _pipe_handler As PipeHandler
    Private _proc As Process
    Private _exception As Exception
    Private ReadOnly _thread As New Threading.Thread(AddressOf Me._loop_internal)

    Public Sub New()
        Me.ID = Utility.IdentificationGenerator.GetNext(Of TaskServer)
        Logger.Debug("Created task server [" & Me.ID & "].")
    End Sub

    Private Sub _change_state(newState As TaskState)
        Me._current_state = newState
        Logger.Information("Task server [" & Me.ID & "] changed state to '" & Me._current_state.ToString & "'.")
        RaiseEvent StateChange(Me, New TaskEventArgs(newState))
    End Sub

    Private Sub _send_message(command As String, data As String, Optional timeout As Double = 1000)
        Me._pipe_handler.SendMessageSafe(If(data = "", {command}, Protocol.BuildStringCommand(command, data)))
    End Sub

    Public Sub Initialize(engine As TaskEngine, options As TaskOptions)
        Try
            If Me.State = TaskState.Initialized Or Me.State = TaskState.Initializing Then Exit Sub
            If Not Me.State = TaskState.Invalid Then _
                Throw New InvalidOperationException("Unable to initialize task: Task is in an invalid state (" & Me.State.ToString & ").")
            Me._engine = engine
            AddHandler Me.StateChange, AddressOf Me._engine.TaskEventSink
            Me._current_state = TaskState.Initializing
            Me._options = options
            Dim pipeName As String = IO.Path.GetRandomFileName()
            Me._pipe_handler = New PipeHandler(
                New NamedPipeServerStream(pipeName & ".in", PipeDirection.In),
                New NamedPipeServerStream(pipeName & ".out", PipeDirection.Out)
            )
            Logger.Debug("Task server [" & Me.ID & "] created IPC pipes [" & pipeName & "].")
            Dim si As New ProcessStartInfo With {
                .Arguments = pipeName,
                .FileName = IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Taas.HostProcess.exe")
            }
            If options.ExecuteWithoutShell Then
                si.CreateNoWindow = True
                si.UseShellExecute = False
            End If
            Me._proc = New Process() With {.StartInfo = si}
            Me._proc.Start()
            Logger.Debug("Task server [" & Me.ID & "] spwawned host process [" & Me._proc.Id & "].")
            Me._thread.Start()
        Catch ex As Exception
            Me._handle_exception(ex)
            Throw New Exception("An exception occured during the initialization stage.", ex)
        End Try
    End Sub

    Public Sub Execute()
        Select Case Me.State
            Case TaskState.Initialized
                Me._change_state(TaskState.Executing)
                Dim thr As New Threading.Thread(
                     Sub()
                         Try : Me._send_message(Protocol.Execute, "")
                         Catch ex As Exception : Me._handle_exception(ex)
                         End Try
                     End Sub
                )
                thr.Start()
            Case TaskState.Paused
                API.APIHost.CurrentProvider.ResumeProcess(Me._proc.Id)
                Me._change_state(TaskState.Executing)
            Case TaskState.Executing
            Case Else
                Throw New InvalidOperationException("Unable to execute task runtime: The task is in an invalid state (" & Me.State.ToString & ").")
        End Select
    End Sub

    Public Sub Abort()
        If Me.State = TaskState.Aborted Then Exit Sub
        If Not (Me.State = TaskState.Executing Or Me.State = TaskState.Paused) Then _
            Throw New InvalidOperationException("Unable to abort task runtime: The task is in an invalid state (" & Me.State.ToString & ").")
        Me._current_state = TaskState.Aborting
        Me._proc.Kill()
    End Sub

    Public Sub Pause()
        If Me.State = TaskState.Paused Then Exit Sub
        If Not Me.State = TaskState.Executing Then _
            Throw New InvalidOperationException("Unable to pause task runtime: The task is in an invalid state (" & Me.State.ToString & ").")
        API.APIHost.CurrentProvider.SuspendProcess(Me._proc.Id)
        Me._change_state(TaskState.Paused)
    End Sub

    Private Sub _loop_internal()
        Try
            DirectCast(Me._pipe_handler.PipeIn, NamedPipeServerStream).WaitForConnection()
            DirectCast(Me._pipe_handler.PipeOut, NamedPipeServerStream).WaitForConnection()
            Me._pipe_handler.PipeOut.WriteByte(Protocol.HandShake)
            Me._pipe_handler.PipeOut.Flush()
            If Not Me._pipe_handler.PipeIn.ReadByte() = Protocol.HandShake Then _
                Throw New Exception("Handshake with host process failed during initialization.")
            Logger.Debug("Task server [" & Me.ID & "] IPC pipe handshake complete.")
            Me._send_message(Protocol.Library, Me._options.Library)
            Me._send_message(Protocol.ClassInfo, Me._options.ClassName)
            Me._change_state(TaskState.Initialized)
            Me._pipe_handler.Run()
            Me._pipe_handler.Close()
            Me._proc.WaitForExit()
            Logger.Debug("Task server [" & Me.ID & "] host process [" & Me._proc.Id & "] exited.")
            If Me._current_state = TaskState.Aborting Then
                Me._change_state(TaskState.Aborted)
            Else
                Me._change_state(TaskState.Finished)
            End If
        Catch thex As Threading.ThreadAbortException
        Catch ex As Exception
            Me._handle_exception(ex)
        End Try
    End Sub

    Public NotOverridable Overrides Function Equals(obj As Object) As Boolean
        Dim retval As Boolean = False
        If GetType(TaskServer).IsAssignableFrom(obj.GetType) Then
            retval = Me._IEquatable_Equals(obj)
        Else
            retval = MyBase.Equals(obj)
        End If
        Return retval
    End Function

    Private Function _IEquatable_Equals(other As TaskServer) As Boolean Implements IEquatable(Of TaskServer).Equals
        Return Me.ID.Equals(other.ID)
    End Function

    Public NotOverridable Overrides Function GetHashCode() As Integer
        Return Me.ID.GetHashCode
    End Function

    Public Overrides Function ToString() As String
        Return Me.GetType.Name & "[" & Me.GetHashCode & "]"
    End Function

    Protected Sub Dispose(disposing As Boolean)
        If Not Me._is_disposed Then
            If disposing Then
                Try
                    If Me._thread.IsAlive Then Me._thread.Abort()
                    If Me._proc IsNot Nothing AndAlso Not Me._proc.HasExited Then
                        Me._proc.Kill()
                        Me._change_state(TaskState.Aborted)
                    End If
                Catch ex As Exception
                Finally
                    Me._change_state(TaskState.Disposed)
                    RemoveHandler Me.StateChange, AddressOf Me._engine.TaskEventSink
                End Try
            End If
        End If
        Me._is_disposed = True
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
    End Sub

    Private Sub _pipe_handler_PipeData(sender As Object, e As PipeDataEventArgs) Handles _pipe_handler.PipeData
        Try
            Select Case e.Data.First
                Case Protocol.Critial
                    Dim data As String = Protocol.GetStringArgument(e.Data)
                    Throw New TaskClientException(data.Split(";").First, "@host process:" & vbNewLine & data.Split(";").Last)
                Case Else
                    Throw New Exception("Invalid token in IPC stream.")
            End Select
        Catch ex As Exception
            Me._handle_exception(ex)
        End Try
    End Sub

    Private Sub _handle_exception(ex As Exception)
        Dim exstring As New Text.StringBuilder
        Dim currentex As Exception = ex
        Do
            exstring.Append(vbNewLine & "(" & ex.GetType.Name & ") " & currentex.Message & vbNewLine & currentex.StackTrace & vbNewLine)
            currentex = currentex.InnerException
        Loop While currentex IsNot Nothing
        Logger.Critical("Task server [" & Me.ID & "] has experienced a critical exception.")
        Logger.Exception("Task server [" & Me.ID & "]: " & exstring.ToString)
        Me._exception = ex
        Try
            If Me._proc IsNot Nothing AndAlso Not Me._proc.HasExited Then
                Me._proc.Kill()
                Me._proc.WaitForExit()
            End If
            If Me._pipe_handler IsNot Nothing Then Me._pipe_handler.Close()
            Me._thread.Abort()
        Catch : End Try
        Me._change_state(TaskState.Failed)
    End Sub

End Class
