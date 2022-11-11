#!/usr/bin/env python3

import hashlib

lines = [line.strip() for line in open("sha512.input").readlines()]
dataLines = [line for line in lines if line and not line.startswith("#")]
hashes = [line.split(':')[1] for line in dataLines]

merkleLeaves = [(leaf, hashlib.sha512(b'\x00' + bytearray.fromhex(leaf)).hexdigest()) for leaf in hashes]
merkleNodes = [(left, right, hashlib.sha512(b'\x01' + bytearray.fromhex(left + right)).hexdigest())
    for left in hashes for right in hashes]

open("sha512-merkle-leaves.input", "w").writelines(':'.join(h) + '\n' for h in merkleLeaves)
open("sha512-merkle-nodes.input", "w").writelines(':'.join(h) + '\n' for h in merkleNodes)
