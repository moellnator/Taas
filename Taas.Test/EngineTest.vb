Imports Taas.BackEnd

<TestClass()> Public Class EngineTest

    <TestMethod()> Public Sub MockSystem_Execute()
        Dim e As New TestEngine
        Using t As New MockTask
            t.Initialize(e)
            Debug.Assert(e.Tasks.Count = 1)
            Debug.Assert(e.Tasks.First.State = TaskState.Initialized)
            t.Execute()
            Debug.Assert(e.Tasks.First.State = TaskState.Running)
            AwaitTask(t, 1000)
            Debug.Assert(e.Tasks.First.State = TaskState.Finished)
        End Using
        Debug.Assert(e.Tasks.First.State = TaskState.Disposed)
    End Sub

    <TestMethod()> Public Sub MockSystem_Fail()
        Dim e As New TestEngine
        Using t As New MockTask
            t.Initialize(e)
            t.Execute()
            t.Fail()
            AwaitTask(t, 1000)
            Debug.Assert(e.Tasks.First.State = TaskState.Failed)
            Debug.Assert(e.LastError.Message = "Task failed.")
        End Using
        Debug.Assert(e.Tasks.First.State = TaskState.Disposed)
    End Sub

    <TestMethod()> Public Sub MockSystem_Abort()
        Dim e As New TestEngine
        Using t As New MockTask
            t.Initialize(e)
            t.Execute()
            t.Abort()
            AwaitTask(t, 1000)
            Debug.Assert(e.Tasks.First.State = TaskState.Aborted)
        End Using
        Debug.Assert(e.Tasks.First.State = TaskState.Disposed)
    End Sub

    Private Sub AwaitTask(task As Task, timeOut As Double)
        Dim stopWatch As New Stopwatch
        stopWatch.Start()
        While task.State = TaskState.Running
            Threading.Thread.Sleep(1)
            If stopWatch.ElapsedMilliseconds >= timeOut Then Throw New Exception("Waiting for task has timed out (" & stopWatch.ElapsedTicks & ").")
        End While
        stopWatch.Stop()
    End Sub

    Public Class MockTask : Inherits Task

        Private _should_fail As Boolean = False

        Protected Overrides Sub Runtime()
            Dim stopWatch As New Stopwatch
            stopWatch.Start()
            While stopWatch.ElapsedMilliseconds <= 100
                Threading.Thread.Sleep(1)
                If Me._should_fail Then Throw New Exception("Task failed.")
            End While
            stopWatch.Stop()
        End Sub

        Public Sub Fail()
            Me._should_fail = True
        End Sub

    End Class

    Public Class TestEngine : Inherits Engine

        Private _tasks As New List(Of Task)
        Public ReadOnly Property Tasks As IReadOnlyList(Of Task)
            Get
                Return Me._tasks
            End Get
        End Property

        Private _last_error As Exception = Nothing
        Public ReadOnly Property LastError As Exception
            Get
                Return _last_error
            End Get
        End Property

        Public Overrides Sub TaskEventSink(sender As Object, e As TaskEventArgs)
            If Not Me._tasks.Contains(sender) Then
                Me._tasks.Add(sender)
            End If
            If e.NewState = TaskState.Failed AndAlso TypeOf e Is TaskExceptionEventArgs Then
                Me._last_error = DirectCast(e, TaskExceptionEventArgs).Exception
            End If
        End Sub

    End Class


End Class