"""Reproducible placeholder laptop artwork. Keep PNG .meta when replacing art."""
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[2]
out = root / 'Assets/Art/UI/Events'
out.mkdir(parents=True, exist_ok=True)
s = 2
im = Image.new('RGBA', (1120*s, 760*s))
d = ImageDraw.Draw(im)
def box(rect, fill, radius=0, outline=None, width=1):
    d.rounded_rectangle(tuple(int(v*s) for v in rect), radius*s, fill=fill, outline=outline, width=width*s)
def line(points, fill, width=1):
    d.line([(int(x*s),int(y*s)) for x,y in points], fill=fill,width=width*s)
box((55,35,1065,607), '#121918', 26)
box((63,38,1057,595), '#424b43', 22, '#77816a', 2)
box((78,52,1042,580), '#242e29', 17, '#111a17', 3)
box((98,72,1022,559), '#111f1d', 8, '#7e8870', 1)
# Quiet screen detail and status indicators; all actual copy is TMP.
line([(126,202),(994,202)], '#44564b')
line([(126,468),(994,468)], '#34483e')
for x,c in [(964,'#a9c78a'),(978,'#688263'),(992,'#455a49')]:
    box((x,93,x+6,99),c,3)
box((533,56,587,61),'#0d1612',2)
box((100,586,1020,608),'#1e2a23',5,'#53604d',1)
d.polygon([(65*s,607*s),(1055*s,607*s),(1110*s,719*s),(1078*s,741*s),(42*s,741*s),(10*s,719*s)], fill='#58614f')
line([(65,607),(1055,607),(1110,719),(10,719),(65,607)],'#8d967c',2)
box((145,619,975,691),'#2a362b',7,'#78856a',1)
for row in range(3):
    for col in range(17):
        x=156+col*47; y=626+row*20
        box((x,y,x+39,y+14),'#414f3e',2,'#67775a',1)
box((444,696,676,726),'#46533e',5,'#8b9875',1)
line([(461,711),(659,711)],'#5c6c4d')
box((67,728,147,732),'#253323',2)
box((973,728,1053,732),'#253323',2)
im.resize((1120,760),Image.Resampling.LANCZOS).save(out/'laptop_frame.png')
for name,fill,stroke in [('normal','#273b31','#7f9971'),('hover','#3a5140','#c7d6a7'),('pressed','#17291f','#9cad80')]:
    b=Image.new('RGBA',(360,112)); bd=ImageDraw.Draw(b)
    bd.rounded_rectangle((2,2,357,109),radius=10,fill=fill,outline=stroke,width=3)
    bd.line((20,94,338,94),fill=stroke,width=1)
    b.resize((180,56),Image.Resampling.LANCZOS).save(out/f'button_{name}.png')
