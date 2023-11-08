namespace WinFormsApp1

open System
open System.Windows.Forms


module Form1 =
    type Form1 () =
       inherit System.Windows.Forms.Form ()
       member this.components : System.ComponentModel.IContainer = null
       interface IDisposable with
           override this.Dispose() =

                if this.components <> null then
                    this.components.Dispose()
                base.Dispose()
        

       // #region Windows Form Designer generated code

       // /// <summary>
       // ///  Required method for Designer support - do not modify
       // ///  the contents of this method with the code editor.
       // /// </summary>
       // private void InitializeComponent()
       // {
       //     this.components = new System.ComponentModel.Container();
       //     this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
       //     this.ClientSize = new System.Drawing.Size(800, 450);
       //     this.Text = "Form1";
       // }

