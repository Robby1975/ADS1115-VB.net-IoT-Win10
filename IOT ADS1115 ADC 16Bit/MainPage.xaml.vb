' The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

Imports System
Imports System.Threading
Imports Windows.UI.Xaml.Controls
Imports Windows.Devices.Enumeration
Imports Windows.Devices.I2C
Imports Windows.Devices.Gpio


Structure Voltage
    Public valueC0 As Double
    Public valueC1 As Double
    Public valueC2 As Double
    Public valueC3 As Double
End Structure


Public NotInheritable Class MainPage
    Inherits Page
    Private Const ADS1115_I2C_ADDR As Byte = &H90          ' 7-bit I2C address Of the ADS1115: 0x48=ADDR pin to GROUND and followed by a low read/write bit*/
    Private ADS1115 As Windows.Devices.I2C.I2cDevice
    Private periodicTimer As Timer




    Private Async Sub Init_I2C_ADS1115()
        Try
            Dim settings = New I2cConnectionSettings(ADS1115_I2C_ADDR >> 1)
            settings.BusSpeed = I2cBusSpeed.FastMode
            Dim aqs As String = I2cDevice.GetDeviceSelector()
            ' Get a selector string that will return all I2C controllers on the system 
            Dim dis = Await DeviceInformation.FindAllAsync(aqs)
            ' Find the I2C bus controller devices with our selector string             
            ADS1115 = Await I2cDevice.FromIdAsync(dis(0).Id, settings)
            ' Create an I2cDevice with our selected bus controller and I2C settings    
            If ADS1115 Is Nothing Then
                Text_Status.Text = String.Format("Slave address {0} on I2C Controller {1} is currently in use by " + "another application. Please ensure that no other applications are using I2C.", settings.SlaveAddress, dis(0).Id)
                Return
            End If

            Dim WriteBuf_setupConfigRegister As Byte() = {&H01, &HC2, &H03}
            ' Write the register settings 
            Try
                ADS1115.Write(WriteBuf_setupConfigRegister)
            Catch ex As Exception
                Debug.WriteLine("Fehler beim schreiben der WriteBuffer")
            End Try
        Catch ex As Exception
            Text_Status.Text = "I2C Initialization failed. Exception: " + ex.Message
            Return
        End Try

        ' Now that everything is initialized, create a timer so we read data every 10mS 

        Dim TimerDelegate As New System.Threading.TimerCallback(AddressOf TimerCallback)
        periodicTimer = New Timer(TimerDelegate, Nothing, 0, 10)

    End Sub


    Private Function Read_I2C_ADS1115() As Voltage
        Dim RegAddrBuf As Byte() = New Byte() {&H0}
        Dim ReadBuf As Byte() = New Byte(2) {}
        Dim WriteBuf_PointToConversation As Byte() = {&H0}

        ADS1115.Write(WriteBuf_PointToConversation) 'Set pointer register to read from conversion register
        ADS1115.Read(ReadBuf)                       'Read from coversion register

        Dim wert = (CShort(ReadBuf(0)) << 8) Or ReadBuf(1) 'Two bytes from coversion register. !!!! Two Complement Format !!!

        Dim volt = (wert * 4.096) / 32767.0
        Dim v As Voltage
        v.valueC0 = volt
        Return v
    End Function



    Private Sub MainPage_Unloaded(sender As Object, args As Object)
        ' Cleanup 
        ADS1115.Dispose()

    End Sub


    Private Sub TimerCallback(state As Object)
        Dim c0Text As String
        Dim statusText As String


        Try
            Dim ADC As Voltage = Read_I2C_ADS1115()
            c0Text = String.Format("Channel 0: {0:F3}V", ADC.valueC0)
            statusText = "Status: Running"
        Catch ex As Exception
            c0Text = "ADS1115: Error"
            statusText = "Failed to read from ADS1115: " & ex.Message
        End Try

        '/* UI updates must be invoked on the UI thread */
        Dim Task = Me.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, Sub()
                                                                                             Text_C0.Text = c0Text
                                                                                             Text_Status.Text = statusText
                                                                                         End Sub)


    End Sub




    Private Sub MainPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Me.InitializeComponent()
        Try
            ADS1115.Dispose()

        Catch ex As Exception

        End Try

        'Register for the unloaded event so we can clean up upon exit
        AddHandler Unloaded, AddressOf MainPage_Unloaded

        ' Initialize the I2C bus, ADC/DCA, And timer 
        Init_I2C_ADS1115()
    End Sub

    Private Sub button_Click(sender As Object, e As RoutedEventArgs) Handles button.Click
        Me.InitializeComponent()
        Try
            ADS1115.Dispose()
        Catch ex As Exception
        End Try

        'Register for the unloaded event so we can clean up upon exit
        AddHandler Unloaded, AddressOf MainPage_Unloaded
        ' Initialize the I2C bus, ADC, And timer 
        Init_I2C_ADS1115()
    End Sub
End Class

