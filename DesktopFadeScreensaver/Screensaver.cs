using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

// TIP: After build change executable extension from EXE to SCR.

static class Screensaver {

    static Form Wnd;
    static PictureBox Img;
    static Timer GlobalTimer;

    [STAThread]
    static void Main(string[] args) {
        
        if(args.Length < 1 || args[0].ToLower() != "/s") return;

        string wallpaper, backColor;
        int wallStyle;
        bool isTiled;

        try {
            wallpaper   = (string)Registry.GetValue("HKEY_CURRENT_USER\\Control Panel\\Desktop\\", "Wallpaper", "");
            backColor   = (string)Registry.GetValue("HKEY_CURRENT_USER\\Control Panel\\Colors\\", "Background", "");
            wallStyle   = int.Parse((string)Registry.GetValue("HKEY_CURRENT_USER\\Control Panel\\Desktop\\", "WallpaperStyle", ""));
            isTiled     = Convert.ToBoolean(int.Parse((string)Registry.GetValue("HKEY_CURRENT_USER\\Control Panel\\Desktop\\", "TileWallpaper", "")));
        } catch {
            LogError("Can not read registry values.");
            return;
        }

        byte r = 0, g = 0, b = 0;

        try {
            var pos = 0;
            var ind = backColor.IndexOf(' ', pos);
            byte.TryParse(backColor.Substring(pos, ind - pos), out r);
            pos = ind + 1;
            ind = backColor.IndexOf(' ', pos);
            byte.TryParse(backColor.Substring(pos, ind - pos), out g);
            pos = ind + 1;
            byte.TryParse(backColor.Substring(pos), out b);
        } catch { }

        Wnd = new Form();
        Wnd.Text = "DesktopFadeScreensaver";
        Wnd.FormBorderStyle = FormBorderStyle.None;
        Wnd.ShowInTaskbar = false;
        Wnd.TopLevel = true;
        Wnd.TopMost = true;
        Wnd.WindowState = FormWindowState.Maximized;
        Wnd.Opacity = 0.0;
        Wnd.BackColor = Color.FromArgb(r, g, b);

        Img = new PictureBox();
        Img.Dock = DockStyle.Fill;
        Wnd.Controls.Add(Img);

        if(File.Exists(wallpaper)) {
            Bitmap bmp = null;

            try {
                bmp = new Bitmap(wallpaper);
            } catch {
                LogError("Can not open wallpaper file.");
                return;
            }

            if(wallStyle == 0) {
                if(isTiled) {
                    Img.BackgroundImage = bmp;
                    Img.BackgroundImageLayout = ImageLayout.Tile;
                } else {
                    Img.Image = bmp;
                    Img.SizeMode = PictureBoxSizeMode.CenterImage;
                }
            } else if(wallStyle == 2) {
                Img.BackgroundImage = bmp;
                Img.BackgroundImageLayout = ImageLayout.Stretch;
            } else if(wallStyle == 6) {
                Img.BackgroundImage = bmp;
                Img.BackgroundImageLayout = ImageLayout.Zoom;
            } else if(wallStyle == 10 || wallStyle == 22) {
                double dx = 0, dy = 0;
                double coef = (double)bmp.Width / bmp.Height > (double)Screen.PrimaryScreen.Bounds.Width / Screen.PrimaryScreen.Bounds.Height ? (double)bmp.Height / Screen.PrimaryScreen.Bounds.Height : (double)bmp.Width / Screen.PrimaryScreen.Bounds.Width;

                dx = Math.Max(0, bmp.Width - Screen.PrimaryScreen.Bounds.Width * coef);
                dy = Math.Max(0, bmp.Height - Screen.PrimaryScreen.Bounds.Height * coef);

                Img.BackgroundImage = bmp.Clone(new RectangleF((float)(dx / 2), (float)(dy / (wallStyle == 10 ? 3 : 2)), (float)(bmp.Width - dx), (float)(bmp.Height - dy)), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Img.BackgroundImageLayout = ImageLayout.Stretch;
                bmp.Dispose();
            }

        }

        Wnd.Load += Wnd_Load;

        Application.Run(Wnd);
    }

    static void Wnd_Load(object sender, EventArgs e) {
        Cursor.Hide();

        GlobalTimer = new Timer();
        GlobalTimer.Interval = 10;
        GlobalTimer.Tick += FadeIn;
        GlobalTimer.Start();

        Img.Click += Click;
        Img.MouseMove += MouseMove;
        Wnd.KeyDown += Click;

        X = Cursor.Position.X;
        Y = Cursor.Position.Y;

    }

    static double X, Y;
    static void MouseMove(object sender, MouseEventArgs e) {
        if(Math.Abs(X - e.X) < 5 && Math.Abs(Y - e.Y) < 5) {
            X = e.X;
            Y = e.Y;
        } else {
            ((Control)sender).MouseMove -= MouseMove;
            Intercept();
        }
    }

    static void Click(object sender, EventArgs e) {
        ((Control)sender).MouseClick -= Click;
        ((Control)sender).KeyDown -= Click;
        Intercept();
    }

    static void Intercept() {
        Cursor.Show();

        GlobalTimer.Stop();
        GlobalTimer.Dispose();

        GlobalTimer = new Timer();
        GlobalTimer.Interval = 10;
        GlobalTimer.Tick += FadeOut;
        GlobalTimer.Start();
    }

    static void FadeIn(object sender, EventArgs e) {
        if(Wnd.Opacity < 0.99) {
            Wnd.Opacity += 0.01;
        } else {
            GlobalTimer.Stop();
        }
    }

    static void FadeOut(object sender, EventArgs e) {
        if(Wnd.Opacity > 0.0) {
            Wnd.Opacity -= 0.01;
        } else {
            GlobalTimer.Stop();
            Application.Exit();
        }
    }

    static void LogError(string msg) {
        File.AppendAllText(Environment.GetEnvironmentVariable("userprofile") + "\\Desktop\\DesktopFadeScreensaver.log", "[" + DateTime.Now.ToString() + "] ERROR: " + msg + "\n");
    }
}
