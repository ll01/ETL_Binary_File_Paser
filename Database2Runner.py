import codecs
import bitstring
from PIL import Image, ImageEnhance
 
t56s = '0123456789[#@:>? ABCDEFGHI&.](<  JKLMNOPQR-$*);\'|/STUVWXYZ ,%="!'
def T56(c):
    return t56s[c]
 
with codecs.open('co59-utf8.txt', 'r', 'utf-8') as co59f:
    co59t = co59f.read()
co59l = co59t.split()
CO59 = {}
for c in co59l:
    ch = c.split(':')
    co = ch[1].split(',')
    CO59[(int(co[0]),int(co[1]))] = ch[0]
 
filename = 'ETL2_1'
skip = 0
 
f = bitstring.ConstBitStream(filename=filename)
f.pos = skip * 6 * 3660
r = f.readlist('int:36,uint:6,pad:30,6*uint:6,6*uint:6,pad:24,2*uint:6,pad:180,bytes:2700') 
print(r[0], T56(r[1]), "".join(map(T56, r[2:8])), "".join(map(T56, r[8:14])), CO59[tuple(r[14:16])])
iF = Image.frombytes('F', (60,60), r[16], 'bit', 6)
iP = iF.convert('P')
fn = '{:d}.png'.format(r[0])
#iP.save(fn, 'PNG', bits=6)
enhancer = ImageEnhance.Brightness(iP)
iE = enhancer.enhance(4)
iE.save(fn, 'PNG')