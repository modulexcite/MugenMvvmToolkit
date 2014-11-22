﻿<phone:PhoneApplicationPage
    x:Class="$rootnamespace$.Views.MainView"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:phone="clr-namespace:Microsoft.Phone.Controls;assembly=Microsoft.Phone"
    xmlns:shell="clr-namespace:Microsoft.Phone.Shell;assembly=Microsoft.Phone"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    FontFamily="{StaticResource PhoneFontFamilyNormal}"
    FontSize="{StaticResource PhoneFontSizeNormal}"
    Foreground="{StaticResource PhoneForegroundBrush}"
    SupportedOrientations="Portrait" Orientation="Portrait"
    mc:Ignorable="d"
    shell:SystemTray.IsVisible="True">

    <!--LayoutRoot is the root grid where all page content is placed-->
    <Grid x:Name="LayoutRoot" Background="Transparent">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <!--TitlePanel contains the name of the application and page title-->
        <StackPanel Grid.Row="0" Margin="12,17,0,28">
            <TextBlock Text="MUGEN MVVM APPLICATION" Style="{StaticResource PhoneTextNormalStyle}" />
            <TextBlock Text="main view" Margin="9,-7,0,0" Style="{StaticResource PhoneTextTitle1Style}" />
        </StackPanel>

        <!--ContentPanel - place additional content here-->
        <Viewbox x:Name="ContentPanel" Grid.Row="1" Margin="12,0,12,0">
            <TextBlock Text="{Binding Path=Text}" />
        </Viewbox>
    </Grid>

</phone:PhoneApplicationPage>