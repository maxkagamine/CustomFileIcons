const fs = require('fs');
const path = require('path');
const icongen = require('icon-gen');
const crc = require('crc');

const ICONS_DIR = path.join(__dirname, '../icons');

let args = process.argv.slice(2);

if (args.length != 2) {
  console.error('Usage: index.js <svg path> <name>');
  process.exit();
}

let svg = args[0];
let hash = crc.crc32(fs.readFileSync(svg)).toString(16);
let name = `${args[1]}.${hash}`;

if (fs.existsSync(path.join(ICONS_DIR, name + '.ico'))) {
  console.log('Icon already exists with same name and svg hash.');
  process.exit();
}

icongen(svg, ICONS_DIR, {
  modes: [ 'ico' ],
  names: { ico: name }
}).then(function() {
  console.log(`Created ${name}.ico`);
}).catch(function(err) {
  console.error(err);
  process.exit(1);
});
