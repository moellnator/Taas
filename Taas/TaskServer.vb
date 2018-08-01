Imports Taas.BackEnd

Public Enum TaskState
    Invalid = -1
    Initialized
    Disposed
    Running
    Finished
    Failed
    Paused
    Aborted
End Enum

Public Class TaskServer : Implements IDisposable, IEquatable(Of TaskServer)

    'TODO: Add finished handler
    'TODO: Add process exception handler

    Private Shared _ID_GLOBAL As Integer = 0
    Private Shared Function _ID_NEXT() As Integer
        Dim retval As Integer = _ID_GLOBAL
        _ID_GLOBAL += 1
        Return retval
    End Function

    Public Event StateChange As TaskEventHandler

    Public ReadOnly Property ID As Integer
    Public ReadOnly Property State As TaskState
        Get
            Return Me._current_state
        End Get
    End Property

    Private _is_disposed As Boolean
    Private _engine As TaskEngine
    Private _current_state As TaskState = TaskState.Invalid
    Private _options As TaskOptions = Nothing
    Private WithEvents _pipe_handler As TaskPipe
    Private _proc As Process
    Private _is_killing As Boolean = False
    Private ReadOnly _thread As New Threading.Thread(AddressOf Me._loop_internal)
    Private ReadOnly _lock As New Threading.Semaphore(0, 1)

    Public Sub New()
        Me.ID = _ID_NEXT()
    End Sub

    Private Sub _change_state(newState As TaskState)
        Me._current_state = newState
        RaiseEvent StateChange(Me, New TaskEventArgs(newState))
    End Sub

    Public Sub Initialize(engine As TaskEngine, options As TaskOptions)
        Me._engine = engine
        AddHandler Me.StateChange, AddressOf Me._engine.TaskEventSink
        Me._options = options
        Dim pipeName As String = IO.Path.GetRandomFileName()
        Me._pipe_handler = New TaskPipe(
            New IO.Pipes.NamedPipeServerStream(pipeName & ".in", IO.Pipes.PipeDirection.In),
            New IO.Pipes.NamedPipeServerStream(pipeName & ".out", IO.Pipes.PipeDirection.Out)
        )
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
        Me._thread.Start()
        Me._lock.WaitOne()
        If Not Me.State = TaskState.Failed Then Me._change_state(TaskState.Initialized)
    End Sub

    Public Sub Execute()
        Select Case Me.State
            Case TaskState.Initialized
                'TODO: Make this timeout enabled
                Me._pipe_handler.SendMessage(TaskPipeProtocol.BuildStringCommand(TaskPipeProtocol.Library, Me._options.Library))
                Me._pipe_handler.SendMessage(TaskPipeProtocol.BuildStringCommand(TaskPipeProtocol.ClassInfo, Me._options.ClassName))
                Me._pipe_handler.SendMessage({TaskPipeProtocol.Execute})
                Me._change_state(TaskState.Running)
            Case TaskState.Paused
                'TODO: Resume Process
                'TODO: Change state
            Case TaskState.Running
            Case Else
                Throw New InvalidOperationException("Unable to execute task runtime: The task is in an invalid state (" & Me.State.ToString & ").")
        End Select
    End Sub

    Public Sub Abort()
        If Me.State = TaskState.Aborted Then Exit Sub
        If Not (Me.State = TaskState.Running Or Me.State = TaskState.Paused) Then _
            Throw New InvalidOperationException("Unable to abort task runtime: The task is in an invalid state (" & Me.State.ToString & ").")
        Me._is_killing = True
        Me._proc.Kill()
    End Sub

    Public Sub Pause()
        If Me.State = TaskState.Paused Then Exit Sub
        If Not Me.State = TaskState.Running Then _
            Throw New InvalidOperationException("Unable to pause task runtime: The task is in an invalid state (" & Me.State.ToString & ").")
        'TODO: Suspend process
        'TODO: Change state
    End Sub

    Private Sub _loop_internal()
        Try
            DirectCast(Me._pipe_handler.PipeIn, IO.Pipes.NamedPipeServerStream).WaitForConnection()
            DirectCast(Me._pipe_handler.PipeOut, IO.Pipes.NamedPipeServerStream).WaitForConnection()
            Me._pipe_handler.PipeOut.WriteByte(TaskPipeProtocol.HandShake)
            Me._pipe_handler.PipeOut.Flush()
            If Not Me._pipe_handler.PipeIn.ReadByte() = TaskPipeProtocol.HandShake Then
                Me._lock.Release()
                Me._change_state(TaskState.Failed)
                'TODO: send an exception and kill HostProcess
                Exit Sub
            End If
            Me._lock.Release()
            Me._pipe_handler.Run()
            Me._pipe_handler.PipeIn.Close()
            Me._pipe_handler.PipeOut.Close()
            Me._proc.WaitForExit()
            If Me._is_killing Then
                Me._change_state(TaskState.Aborted)
            Else
                Me._change_state(TaskState.Finished)
            End If
        Catch thex As Threading.ThreadAbortException
        Catch ex As Exception
            'TODO: set state
            'TODO: kill process
            'TODO: transmit error
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
                'TODO: kill host process and stop thread
                Me._change_state(TaskState.Disposed)
                RemoveHandler Me.StateChange, AddressOf Me._engine.TaskEventSink
            End If
        End If
        Me._is_disposed = True
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
    End Sub

    Private Sub _pipe_handler_PipeData(sender As Object, e As TaskPipeDataEventArgs) Handles _pipe_handler.PipeData
        'TODO: Filter and transmit error messages from the process
    End Sub

End Class
