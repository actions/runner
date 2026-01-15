#!/usr/bin/env node
/**
 * Create simple green circle PNG icons
 * No dependencies required - uses pure JavaScript to create valid PNG files
 */

const fs = require('fs');
const path = require('path');
const zlib = require('zlib');

function createPNG(size) {
  // PNG uses RGBA format, one pixel = 4 bytes
  const pixelData = [];

  const centerX = size / 2;
  const centerY = size / 2;
  const radius = size / 2 - 1;
  const innerRadius = radius * 0.4;

  for (let y = 0; y < size; y++) {
    pixelData.push(0); // Filter byte for each row
    for (let x = 0; x < size; x++) {
      const dx = x - centerX;
      const dy = y - centerY;
      const dist = Math.sqrt(dx * dx + dy * dy);

      if (dist <= radius) {
        // Green circle (#238636)
        pixelData.push(35, 134, 54, 255);
      } else {
        // Transparent
        pixelData.push(0, 0, 0, 0);
      }
    }
  }

  // Add a white "bug" shape in the center
  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      const dx = x - centerX;
      const dy = y - centerY;
      const dist = Math.sqrt(dx * dx + dy * dy);

      // Bug body (oval)
      const bodyDx = dx;
      const bodyDy = (dy - size * 0.05) / 1.3;
      const bodyDist = Math.sqrt(bodyDx * bodyDx + bodyDy * bodyDy);

      // Bug head (circle above body)
      const headDx = dx;
      const headDy = dy + size * 0.15;
      const headDist = Math.sqrt(headDx * headDx + headDy * headDy);

      if (bodyDist < innerRadius || headDist < innerRadius * 0.6) {
        const idx = 1 + y * (1 + size * 4) + x * 4;
        pixelData[idx] = 255;
        pixelData[idx + 1] = 255;
        pixelData[idx + 2] = 255;
        pixelData[idx + 3] = 255;
      }
    }
  }

  const rawData = Buffer.from(pixelData);
  const compressed = zlib.deflateSync(rawData);

  // Build PNG file
  const chunks = [];

  // PNG signature
  chunks.push(Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]));

  // IHDR chunk
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(size, 0); // width
  ihdr.writeUInt32BE(size, 4); // height
  ihdr.writeUInt8(8, 8); // bit depth
  ihdr.writeUInt8(6, 9); // color type (RGBA)
  ihdr.writeUInt8(0, 10); // compression
  ihdr.writeUInt8(0, 11); // filter
  ihdr.writeUInt8(0, 12); // interlace
  chunks.push(createChunk('IHDR', ihdr));

  // IDAT chunk
  chunks.push(createChunk('IDAT', compressed));

  // IEND chunk
  chunks.push(createChunk('IEND', Buffer.alloc(0)));

  return Buffer.concat(chunks);
}

function createChunk(type, data) {
  const typeBuffer = Buffer.from(type);
  const length = Buffer.alloc(4);
  length.writeUInt32BE(data.length, 0);

  const crcData = Buffer.concat([typeBuffer, data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(crcData), 0);

  return Buffer.concat([length, typeBuffer, data, crc]);
}

// CRC32 implementation
function crc32(buf) {
  let crc = 0xffffffff;
  for (let i = 0; i < buf.length; i++) {
    crc = crc32Table[(crc ^ buf[i]) & 0xff] ^ (crc >>> 8);
  }
  return (crc ^ 0xffffffff) >>> 0;
}

// CRC32 lookup table
const crc32Table = new Uint32Array(256);
for (let i = 0; i < 256; i++) {
  let c = i;
  for (let j = 0; j < 8; j++) {
    c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
  }
  crc32Table[i] = c;
}

// Generate icons
const iconsDir = path.join(__dirname);
const sizes = [16, 48, 128];

sizes.forEach((size) => {
  const png = createPNG(size);
  const filename = `icon${size}.png`;
  fs.writeFileSync(path.join(iconsDir, filename), png);
  console.log(`Created ${filename} (${size}x${size})`);
});

console.log('Done!');
