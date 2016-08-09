Imports AForge.Video
Imports AForge.Video.DirectShow
Imports System.ComponentModel
Imports System.Runtime.InteropServices

Public Class ConvertCameraToAsciiForm
    Private VideoDevicesList As FilterInfoCollection = New FilterInfoCollection(FilterCategory.VideoInputDevice)
    Private VideoSource As IVideoSource = New VideoCaptureDevice(VideoDevicesList(0).MonikerString) '使用默认设备
    Dim CellSize As Size = New Size(5, 10)
    Dim FormSize As Size = New Size(640, 360)
    Dim AsciiCount As Int16 = 32
    Dim Ascii() As Char = {" ", "`", ".", "^", ",", ":", "~", """", "<", "!", "c", "t", "+", "{", "i", "7", "?", "u", "3", "0", "p", "w", "4", "A", "8", "D", "X", "%", "#", "H", "W", "M"}
    Dim AscGray() As Byte = {0, 14, 19, 25, 36, 42, 48, 53, 59, 65, 70, 76, 82, 87, 93, 99, 104, 110, 116, 121, 127, 133, 138, 144, 150, 155, 167, 172, 178, 187, 192, 198}
    Dim FrameBitmap As Bitmap



    Private Function GrayScaleBitmap(ByVal InitialBitmap As Bitmap) As Bitmap
        Dim GrayBitmapData As Imaging.BitmapData = New Imaging.BitmapData
        GrayBitmapData = InitialBitmap.LockBits(New Rectangle(0, 0, InitialBitmap.Width, InitialBitmap.Height),
                Imaging.ImageLockMode.WriteOnly, InitialBitmap.PixelFormat)
        Dim DataStride As Integer = GrayBitmapData.Stride
        Dim DataHeight As Integer = GrayBitmapData.Height
        Dim GrayDataArray(DataStride * DataHeight - 1) As Byte
        Marshal.Copy(GrayBitmapData.Scan0, GrayDataArray, 0, GrayDataArray.Length)

        Dim Index As Integer, Gray As Byte
        For Index = 0 To GrayDataArray.Length - 1 Step 4
            Gray = GrayDataArray(Index + 2) * 0.229 + GrayDataArray(Index + 1) * 0.587 + GrayDataArray(Index + 0) * 0.114
            GrayDataArray(Index + 0) = Gray
            GrayDataArray(Index + 1) = Gray
            GrayDataArray(Index + 2) = Gray
        Next

        Marshal.Copy(GrayDataArray, 0, GrayBitmapData.Scan0, GrayDataArray.Length)
        InitialBitmap.UnlockBits(GrayBitmapData)
        Return InitialBitmap
    End Function

    Private Function BitmapToAscii(ByVal GrayScaleBitmap As Bitmap, ByVal CellSize As Size) As String
        Dim AsciiString As String = vbNullString
        Dim IndexX, IndexY As Integer
        Dim CellBitmap As Bitmap
        Dim CellGrayScale As Byte
        For IndexY = 0 To GrayScaleBitmap.Height - 1 - CellSize.Height Step CellSize.Height
            For IndexX = 0 To GrayScaleBitmap.Width - 1 - CellSize.Width Step CellSize.Width
                CellBitmap = GrayScaleBitmap.Clone(New Rectangle(IndexX, IndexY, CellSize.Width, CellSize.Height), Imaging.PixelFormat.Format32bppArgb)
                CellBitmap = New Bitmap(CellBitmap, 1, 1)
                CellGrayScale = 255 - CellBitmap.GetPixel(0, 0).R
                AsciiString &= AsciiFromGrayScale(CellGrayScale)
            Next
            AsciiString &= vbCrLf
        Next

        Return Strings.Left(AsciiString, AsciiString.Length - 1)
    End Function

    Private Function AsciiFromGrayScale(ByVal GrayScale As Byte) As Char
        Dim lower As Byte = 0
        Dim higher As Byte = AsciiCount
        Dim Mid As Byte
        While ((higher - lower) > 1)
            Mid = (lower + higher) / 2
            If (GrayScale > AscGray(Mid)) Then
                lower = Mid
            Else
                higher = Mid
            End If
        End While
        Return Ascii(lower)
    End Function

    Private Sub Form1_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        VideoSource.SignalToStop() '关闭摄像头
        Application.Exit() '退出程序
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        CheckForIllegalCrossThreadCalls = False
        Me.BackgroundImageLayout = ImageLayout.Zoom
        Me.DoubleBuffered = True
        '绑定画面刷新事件
        AddHandler VideoSource.NewFrame, AddressOf video_NewFrame
        '开启摄像头
        VideoSource.Start()
    End Sub

    Private Sub video_NewFrame(sender As Object, eventArgs As NewFrameEventArgs)
        Static Index As Integer = 0
        Index += 1
        FrameBitmap = New Bitmap(eventArgs.Frame, FormSize)
        FrameBitmap = GrayScaleBitmap(FrameBitmap)
        TextBox1.Text = BitmapToAscii(FrameBitmap, CellSize)
        '强制内存回收
        GC.Collect()
    End Sub
End Class
