import codecs
import os, errno
import bitstring
import glob
import sys
from PIL import Image, ImageEnhance

def main(source, destination):
    t56s = '0123456789[#@:>? ABCDEFGHI&.](<  JKLMNOPQR-$*);\'|/STUVWXYZ ,%="!'
    def T56(c):
        return t56s[c]
    
    with codecs.open('co59-utf8.txt', 'r', 'utf-8') as co59f:
        print(co59f)
        co59t = co59f.read()
    co59l = co59t.split()
    CO59 = {}
    for c in co59l:
        ch = c.split(':')
        co = ch[1].split(',')
        CO59[(int(co[0]),int(co[1]))] = ch[0]


    files = glob.glob(source)
    # filename = 'E:\kanji dataset\extract\ETL2\ETL2_1'
    filename = source
    #skip = 0
    f = bitstring.ConstBitStream(filename=filename)
    for skip in range(11420):
        f.pos = skip * 6 * 3660
        r = f.readlist('int:36,uint:6,pad:30,6*uint:6,6*uint:6,pad:24,2*uint:6,pad:180,bytes:2700') 
        # print(r[0], T56(r[1]), "".join(map(T56, r[2:8])), "".join(map(T56, r[8:14])), CO59[tuple(r[14:16])])
        iF = Image.frombytes('F', (60,60), r[16], 'bit', 6)
        iP = iF.convert('L')
        imagefilepath = destination+ '/%s' % CO59[tuple(r[14:16])]
        if not os.path.exists(imagefilepath):
            os.makedirs(imagefilepath)
        fn = '%s/{:d}.png'.format(r[0]) % imagefilepath
        iP.save(fn, 'PNG', bits=6)
        enhancer = ImageEnhance.Brightness(iP)
        iE = enhancer.enhance(4)
        iE.save(fn, 'PNG')
 
if __name__ == "__main__":
    source  = sys.argv[1]
    destination  = sys.argv[2]
    print(source)
    main(source, destination)


# Python
# import struct
# from PIL import Image, ImageEnhance

# filename = 'ETL1/ETL1C_01'
# skip = 100
# with open(filename, 'rb') as f:
#     f.seek(skip * 2052)
#     s = f.read(2052)
#     r = struct.unpack('>H2sH6BI4H4B4x2016s4x', s)
#     iF = Image.frombytes('F', (64, 63), r[18], 'bit', 4)
#     iP = iF.convert('P')
#     fn = "{:1d}{:4d}{:2x}.png".format(r[0], r[2], r[3])
# #    iP.save(fn, 'PNG', bits=4)
#     enhancer = ImageEnhance.Brightness(iP)
#     iE = enhancer.enhance(16)
#     iE.save(fn, 'PNG')

