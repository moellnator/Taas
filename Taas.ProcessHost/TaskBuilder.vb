Public Class TaskBuilder

    Private _asm As Reflection.Assembly
    Private _type As Type
    Private _instance As TaskPayload

    Public ReadOnly Property Assembly As Reflection.Assembly
        Get
            Return Me._asm
        End Get
    End Property

    Public ReadOnly Property InstanceType As Type
        Get
            Return Me._type
        End Get
    End Property

    Public Sub SetLibrary(library As String)
        Me._asm = Reflection.Assembly.LoadFrom(library)
    End Sub

    Public Sub BuildClass(className As String)
        Dim retval As Type = Nothing
        For Each c As Type In Me._asm.GetTypes
            If c.FullName = className Then
                If GetType(TaskPayload).IsAssignableFrom(c) Then
                    retval = c
                    Exit For
                Else
                    Throw New ArgumentException("Cannot load the specified class: Not a valid payload.")
                End If
            End If
        Next
        If retval Is Nothing Then Throw New ArgumentException("Cannot lad the specified class: Class not found in assembly.")
        Me._type = retval
        Me._instance = Me._type.GetConstructor(New Type() {}).Invoke(New Object() {})
    End Sub

    Public Sub Execute()
        Me._instance.Execute()
    End Sub

End Class
