   M S B A M L   `   `   `  ����E  ANewsClient, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null3clr-namespace:NewsClient.ViewsNewsClient.Views  5      =#clr-namespace:NewsClient.ViewModelsNewsClient.ViewModels  5      !    NewsClient.Views.AuthWindow   5      W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      0viewsclr-namespace:NewsClient.Views     5      2vm#clr-namespace:NewsClient.ViewModels     5        � Title$"  Аутентификация��5      $	��350��6*   $	��400��67    � 
ResizeMode$ NoResize=�5       � WindowStartupLocation$ CenterScreen=�6)    � DataContext 5      )   #NewsClient.ViewModels.AuthViewModel  5	   
   .��5      � $��20q�6    � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $��10 �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $��10 �6   �� 5      $
��Auto �6   �� 5      $��10 �6   �� 5      $
��Auto �6   .+�5   
   �� $��0��6   $��Email��6!   
�������$��Center=�5      $��5,0,0,0q�6/   $
��Bold"�6@   �� 5   
   $��1��6   $��30��5      $��0,0,0,0q�6   $��Center=�6/   $��5q�5      ���#B0B0B0$��1q�64   $��Stretch=�5      ��5      �� *Email+ �� UpdateSourceTrigger$ PropertyChanged=�5      �� 5"   
   $��3��6   $��Пароль��6!   
�������$��Center=�5#      $��5,0,0,0q�6/   $
��Bold"�6@   G� 5$   
   -   5         G�Name$ txtPassword��5$   #   $��4��6   $��30��68   $��0,0,0,0q�6D   $��5q�5%      ���#B0B0B0$��1q�68   $��Stretch=�5&      � 5)   
   -   5         �Name$ RepeatPasswordContainer��5)      $��6��6   y�5*      �� *IsRegisterMode+   BoolToVisibilityConverter �� 	Converter# [   5+      �� 5,      $
��Auto �6    �� 5-      $
��Auto �6    .+�5/      �� $��0��6   $%��Повторите пароль��6%   
�������$��Center=�50      $��5,0,0,0q�63   $
��Bold"�6D   G� 51      -   5        $ txtRepeatPassword��51   '   $��1��6   $��30��6B   $��0,5,0,0q�52      $��5q�6+   ���#B0B0B0$��1q�53      $��Stretch=�6.   54      �� 57   
   $��8��6   	 �� Orientation$	 
Horizontal=�6"   $��Center=�58      $��0,20,0,0q�62   .+�59      �� $��Вход��6   $	��120��6$   $��10q�60   ���#4CAF50
����������#4CAF50$��1q�5<   +   $
��10,5q�6?   ��5:      �� *LoginCommand+5<   ?   �� 5=      $��Регистрация��6   $	��120��6+   $��10q�67   ���#2196F3
����������#2196F3$��1q�5@   +   $
��10,5q�6?   ��5>      �� *RegisterCommand+5@   ?   5A      5B      5C      