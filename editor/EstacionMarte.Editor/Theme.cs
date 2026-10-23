namespace EstacionMarte.Editor;
static class Theme
{
    public static readonly Color Paper=Color.FromArgb(248,245,239), Ink=Color.FromArgb(73,51,33), Muted=Color.FromArgb(137,121,102), Gold=Color.FromArgb(182,141,45), Line=Color.FromArgb(222,213,200);
    public static Button Button(string text, EventHandler handler, bool primary=false)
    {
        var b=new Button{Text=text,AutoSize=true,MinimumSize=new Size(120,40),FlatStyle=FlatStyle.Flat,BackColor=primary?Ink:Paper,ForeColor=primary?Paper:Ink,Padding=new Padding(12,5,12,5),Cursor=Cursors.Hand,Margin=new Padding(0,0,12,10)};
        b.FlatAppearance.BorderColor=primary?Ink:Line;b.Click+=handler;return b;
    }
    public static Label Heading(string text,int size=24) => new(){Text=text,AutoSize=true,Font=new Font("Georgia",size),ForeColor=Ink,Margin=new Padding(0,0,0,15)};
    public static Label Note(string text) => new(){Text=text,AutoSize=true,MaximumSize=new Size(780,0),ForeColor=Muted,Margin=new Padding(0,0,0,20)};
    public static TextBox Input(string value="",bool multiline=false) => new(){Text=value,Width=530,Multiline=multiline,Height=multiline?65:30,BorderStyle=BorderStyle.FixedSingle,Margin=new Padding(0,5,0,18)};
    public static void Field(FlowLayoutPanel panel,string label,Control input){panel.Controls.Add(new Label{Text=label,AutoSize=true,ForeColor=Ink,Margin=new Padding(0)});panel.Controls.Add(input);}
}
