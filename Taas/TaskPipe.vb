
Public Class TaskPipe

    Public Event PipeData As TaskPipeDataEvent

    Public ReadOnly Property PipeIn As IO.Pipes.PipeStream
    Public ReadOnly Property PipeOut As IO.Pipes.PipeStream
    Private ReadOnly _buffer_size As Integer
    Private _data_length As Integer = -1
    Private _data_buffer As Byte()

    Public Sub New(pipeIn As IO.Pipes.PipeStream, pipeOut As IO.Pipes.PipeStream, Optional bufferSize As Integer = 1024)
        Me.PipeIn = pipeIn
        Me.PipeOut = pipeOut
        Me._buffer_size = bufferSize
    End Sub

    Public Sub Run()
        Dim buffer As Byte() : ReDim buffer(_buffer_size - 1)
        Dim length As Integer = 0
        While True
            length = Me.PipeIn.Read(buffer, 0, _buffer_size)
            If Not length = 0 Then
                If Me._data_length = -1 Then
                    Dim total As Integer = buffer(0) Or (buffer(1) * &H100)
                    ReDim Me._data_buffer(total - 1)
                    Me._data_length = 0
                Else
                    Array.Copy(buffer, 0, Me._data_buffer, Me._data_length, length)
                    Me._data_length += length
                    If Me._data_length = Me._data_buffer.Count Then
                        Me._data_length = -1
                        RaiseEvent PipeData(Me, New TaskPipeDataEventArgs(Me._data_buffer))
                    End If
                End If
            Else
                Exit While
            End If
        End While
    End Sub

    Public Sub SendMessage(buffer As Byte())
        Dim current As Integer = 0
        Dim total As Integer = Math.Ceiling(buffer.Count / Me._buffer_size)
        Me.PipeOut.Write(New Byte() {buffer.Count And &HFF, (buffer.Count >> 8) And &HFF}, 0, 2)
        Do
            Dim size As Integer = Math.Min(Me._buffer_size, buffer.Count - current)
            Me.PipeOut.Write(buffer, current, size)
            Me.PipeOut.Flush()
            Me.PipeOut.WaitForPipeDrain()
            current += size
        Loop Until current = buffer.Count
    End Sub

End Class
