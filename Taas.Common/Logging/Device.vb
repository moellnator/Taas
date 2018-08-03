Namespace Logging
    Public MustInherit Class Device : Implements IEquatable(Of Device)

        Private ReadOnly _device_id As Integer = Utility.IdentificationGenerator.GetNext(Of Device)
        Public MustOverride Sub AddEntry(entry As Entry)

        Public Overrides Function Equals(obj As Object) As Boolean
            If GetType(Device).IsAssignableFrom(obj.GetType) Then
                Return IEquatable_Equals(obj)
            Else
                Return MyBase.Equals(obj)
            End If
        End Function

        Private Function IEquatable_Equals(other As Device) As Boolean Implements IEquatable(Of Device).Equals
            Return Me._device_id = other._device_id
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Me._device_id.GetHashCode
        End Function

    End Class

End Namespace
