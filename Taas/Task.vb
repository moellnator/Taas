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

Public MustInherit Class Task : Implements IDisposable, IEquatable(Of Task)

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
    Private _thread As New Threading.Thread(AddressOf Me._loop_internal)
    Private _lock As New Threading.Semaphore(0, 1)

    Public Sub New()
        Me.ID = _ID_NEXT()
    End Sub

    Private Sub _change_state(newState As TaskState)
        Me._current_state = newState
        RaiseEvent StateChange(Me, New TaskEventArgs(newState))
    End Sub

    Public Sub Initialize(engine As TaskEngine)
        Me._engine = engine
        AddHandler Me.StateChange, AddressOf Me._engine.TaskEventSink
        Me.Setup()
        Me._change_state(TaskState.Initialized)
    End Sub

    Protected Overridable Sub Setup()
    End Sub

    Public Sub Execute()
        Select Case Me.State
            Case TaskState.Initialized
                Me._thread.Start()
                Me._lock.WaitOne()
            Case TaskState.Paused
                Me.RuntimeResume()
            Case TaskState.Running
            Case Else
                Throw New InvalidOperationException("Unable to execute task runtime: The task is in an invalid state (" & Me.State.ToString & ").")
        End Select
    End Sub

    <CodeAnalysis.SuppressMessage("System.Obsolete", "BC40000")> Protected Overridable Sub RuntimeResume()
        Me._thread.Resume()
        Me._change_state(TaskState.Running)
    End Sub

    Public Sub Abort()
        If Me.State = TaskState.Aborted Then Exit Sub
        If Not (Me.State = TaskState.Running Or Me.State = TaskState.Paused) Then _
            Throw New InvalidOperationException("Unable to abort task runtime: The task is in an invalid state (" & Me.State.ToString & ").")
        Me.RuntimeAbort()
    End Sub

    Protected Overridable Sub RuntimeAbort()
        Me._thread.Abort()
        Me._lock.WaitOne()
    End Sub

    Public Sub Pause()
        If Me.State = TaskState.Paused Then Exit Sub
        If Not Me.State = TaskState.Running Then _
            Throw New InvalidOperationException("Unable to pause task runtime: The task is in an invalid state (" & Me.State.ToString & ").")
        Me.RuntimePause()
    End Sub

    <CodeAnalysis.SuppressMessage("System.Obsolete", "BC40000")> Protected Overridable Sub RuntimePause()
        Me._thread.Suspend()
        Me._change_state(TaskState.Paused)
    End Sub

    Private Sub _loop_internal()
        Try
            Me._lock.Release()
            Me._change_state(TaskState.Running)
            Me.Runtime()
            Me._change_state(TaskState.Finished)
        Catch thex As Threading.ThreadAbortException
            Me._lock.Release()
            Me._change_state(TaskState.Aborted)
        Catch ex As Exception
            Me._current_state = TaskState.Failed
            RaiseEvent StateChange(Me, New TaskExceptionEventArgs(ex))
        End Try
    End Sub

    Protected MustOverride Sub Runtime()

    Public NotOverridable Overrides Function Equals(obj As Object) As Boolean
        Dim retval As Boolean = False
        If GetType(Task).IsAssignableFrom(obj.GetType) Then
            retval = Me._IEquatable_Equals(obj)
        Else
            retval = MyBase.Equals(obj)
        End If
        Return retval
    End Function

    Private Function _IEquatable_Equals(other As Task) As Boolean Implements IEquatable(Of Task).Equals
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
                Me.CleanUp()
                Me._change_state(TaskState.Disposed)
                RemoveHandler Me.StateChange, AddressOf Me._engine.TaskEventSink
            End If
            Me.CleanUpUnmanaged()
        End If
        Me._is_disposed = True
    End Sub

    Protected NotOverridable Overrides Sub Finalize()
        Dispose(False)
        MyBase.Finalize()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

    Protected Overridable Sub CleanUp()
    End Sub

    Protected Overridable Sub CleanUpUnmanaged()
    End Sub

End Class
