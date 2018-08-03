Namespace Logging
    Public Class Entry : Implements IEquatable(Of Entry)

        Private ReadOnly _id As Integer
        Public ReadOnly Property Level As Level
        Public ReadOnly Property Message As String
        Public ReadOnly Property TimeStamp As DateTime

        Public Sub New(level As Level, message As String)
            Me._id = Utility.IdentificationGenerator.GetNext(Of Entry)
            Me.Level = level
            Me.Message = message
            Me.TimeStamp = DateTime.Now
        End Sub

        Public Overrides Function Equals(obj As Object) As Boolean
            Return MyBase.Equals(obj)
        End Function

        Private Function IEquatable_Equals(other As Entry) As Boolean Implements IEquatable(Of Entry).Equals
            Return Me._id = other._id
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Me._id.GetHashCode
        End Function

    End Class

End Namespace
