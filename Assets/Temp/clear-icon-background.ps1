Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Collections.Generic;
public static class IconAlphaRepair {
 public static void Run(string source, string target) {
  using(var src=new Bitmap(source)) using(var dst=new Bitmap(src.Width,src.Height,System.Drawing.Imaging.PixelFormat.Format32bppArgb)) {
   int w=src.Width,h=src.Height; var seen=new bool[w*h]; var clear=new bool[w*h];
   for(int y=0;y<h;y++) for(int x=0;x<w;x++) dst.SetPixel(x,y,src.GetPixel(x,y));
   for(int start=0;start<w*h;start++) {
    if(seen[start]) continue;
    var c=src.GetPixel(start%w,start/w);
    if(!Background(c)){seen[start]=true;continue;}
    var component=new List<int>(); var queue=new Queue<int>();queue.Enqueue(start);seen[start]=true;bool edge=false;
    while(queue.Count>0){int p=queue.Dequeue(),x=p%w,y=p/w;component.Add(p);if(x==0||y==0||x==w-1||y==h-1)edge=true;
     foreach(int n in new int[]{x>0?p-1:-1,x<w-1?p+1:-1,y>0?p-w:-1,y<h-1?p+w:-1}) {
      if(n<0||seen[n])continue;seen[n]=true;if(Background(src.GetPixel(n%w,n/w)))queue.Enqueue(n);
     }
    }
    if(edge||component.Count>1500)foreach(int p in component)clear[p]=true;
   }
   int removed=0;for(int p=0;p<w*h;p++)if(clear[p]){dst.SetPixel(p%w,p/w,Color.FromArgb(0,0,0,0));removed++;}
   dst.Save(target,System.Drawing.Imaging.ImageFormat.Png);Console.WriteLine("Transparent pixels: "+removed+"; corners alpha="+dst.GetPixel(0,0).A);
  }
 }
 static bool Background(Color c){return Math.Max(c.R,Math.Max(c.G,c.B))-Math.Min(c.R,Math.Min(c.G,c.B))<14 && Math.Min(c.R,Math.Min(c.G,c.B))>150;}
}
'@
[IconAlphaRepair]::Run('C:/Users/water/.codex/generated_images/01a00e9e-69b0-79c1-8f68-64ab982cf5fb/exec-bfd24fca-69ad-4250-8137-ee2f2e079b36.png', (Join-Path (Get-Location) 'Art/GameplayHUD/Icons/Review01/icon_resource_parts_03.png'))
