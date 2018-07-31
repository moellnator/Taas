Imports Taas.BackEnd

<TestClass()> Public Class EngineTest

    <TestMethod()> Public Sub MockSystem_Execute()
        Dim e As New TestEngine
        Using t As New MockTask
            t.Initialize(e)
            Assert.AreEqual(e.Tasks.Count, 1)
            Assert.AreEqual(e.Tasks.First.State, TaskState.Initialized)
            t.Execute()
            Assert.AreEqual(e.Tasks.First.State, TaskState.Running)
            AwaitTask(t, 1000)
            Assert.AreEqual(e.Tasks.First.State, TaskState.Finished)
        End Using
        Assert.AreEqual(e.Tasks.First.State, TaskState.Disposed)
    End Sub

    <TestMethod()> Public Sub MockSystem_Fail()
        Dim e As New TestEngine
        Using t As New MockTask
            t.Initialize(e)
            t.Execute()
            t.Fail()
            AwaitTask(t, 1000)
            Assert.AreEqual(e.Tasks.First.State, TaskState.Failed)
            Assert.AreEqual(e.LastError.Message, "Task failed.")
        End Using
        Assert.AreEqual(e.Tasks.First.State, TaskState.Disposed)
    End Sub

    <TestMethod()> Public Sub MockSystem_Abort()
        Dim e As New TestEngine
        Using t As New MockTask
            t.Initialize(e)
            t.Execute()
            t.Abort()
            AwaitTask(t, 1000)
            Assert.AreEqual(e.Tasks.First.State, TaskState.Aborted)
        End Using
        Assert.AreEqual(e.Tasks.First.State, TaskState.Disposed)
    End Sub

    <TestMethod()> Public Sub MockSystem_Pause()
        Dim e As New TestEngine
        Using t As New MockTask
            t.Initialize(e)
            t.Execute()
            t.Pause()
            Assert.AreEqual(e.Tasks.First.State, TaskState.Paused)
            t.Execute()
            AwaitTask(t, 1000)
            Assert.AreEqual(e.Tasks.First.State, TaskState.Finished)
        End Using
        Assert.AreEqual(e.Tasks.First.State, TaskState.Disposed)
    End Sub

    Private Sub AwaitTask(task As Task, timeOut As Double)
        Dim stopWatch As New Stopwatch
        stopWatch.Start()
        While task.State = TaskState.Running
            Threading.Thread.Sleep(1)
            If stopWatch.ElapsedMilliseconds >= timeOut Then Assert.Fail("Waiting for task has timed out (" & stopWatch.ElapsedTicks & ").")
        End While
        stopWatch.Stop()
    End Sub

    Public Class MockTask : Inherits Task

        Private _should_fail As Boolean = False

        Protected Overrides Sub Runtime()
            Trace.WriteLine("Task has been started.")
            Dim stopWatch As New Stopwatch
            stopWatch.Start()
            While stopWatch.ElapsedMilliseconds <= 100
                Threading.Thread.Sleep(1)
                If Me._should_fail Then Throw New Exception("Task failed.")
            End While
            stopWatch.Stop()
            Trace.WriteLine("Task has been executed.")
        End Sub

        Public Sub Fail()
            Me._should_fail = True
        End Sub

        Protected Overrides Sub Setup()
            MyBase.Setup()
            Trace.WriteLine("Task has been setup.")
        End Sub

        Protected Overrides Sub CleanUp()
            MyBase.CleanUp()
            Trace.WriteLine("Task has been cleaned up.")
        End Sub

        Protected Overrides Sub CleanUpUnmanaged()
            MyBase.CleanUp()
            Trace.WriteLine("Task has been cleaned up (unmanaged ressource).")
        End Sub

        Protected Overrides Sub RuntimePause()
            MyBase.RuntimePause()
            Trace.WriteLine("Task has been paused.")
        End Sub

        Protected Overrides Sub RuntimeResume()
            MyBase.RuntimeResume()
            Trace.WriteLine("Task has been resumed.")
        End Sub

        Protected Overrides Sub RuntimeAbort()
            MyBase.RuntimeAbort()
            Trace.WriteLine("Task has been aborted.")
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