Imports System.Reflection
Imports Taas.BackEnd

Public Class TaskRespository

    Private Shared _COMMON As TaskRespository

    Public Shared ReadOnly Property Common As TaskRespository
        Get
            If _COMMON Is Nothing Then
                _COMMON = New TaskRespository
            End If
            Return _COMMON
        End Get
    End Property

    Private _tasks As New Dictionary(Of String, Func(Of Task))

    Public Sub LoadFromAssembly(path As String)
        Dim asm As Assembly = Assembly.LoadFrom(path)
        Dim filename As String = IO.Path.GetFileName(path)
        For Each t As Type In asm.GetTypes
            Dim attr As Attribute() = t.GetCustomAttributes
            If attr.Any(Function(a) TypeOf a Is TaskAttribute) Then
                If GetType(Task).IsAssignableFrom(t) Then
                    Dim ci As ConstructorInfo = t.GetConstructor(New Type() {})
                    Me._tasks.Add(filename & ":" & t.FullName.Replace("+", "."), Function() ci.Invoke(New Object() {}))
                End If
            End If
        Next
    End Sub

    Public Function Instanciate(fullName As String) As Task
        Return Me._tasks(fullName).Invoke()
    End Function

    Private Sub New()
    End Sub

    Public ReadOnly Property GetListOfTaskNames() As IEnumerable(Of String)
        Get
            Return Me._tasks.Keys
        End Get
    End Property

    Public Sub Reset()
        Me._tasks.Clear()
    End Sub

End Class
