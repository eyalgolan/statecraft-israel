"""#1119: safe margins are relative to the canvas after the page reserves space beside/below it."""
import json
import shutil
import subprocess
from pathlib import Path

import pytest

SOURCE = Path(__file__).resolve().parents[2] / "unity/Assets/Scripts/UI/SafeArea.jslib"


@pytest.mark.skipif(shutil.which("node") is None, reason="needs node for the shipped .jslib")
@pytest.mark.parametrize("rect,insets,expected", [
    ([0, 0, 390, 844], [47, 0, 34, 0], [47, 0, 34, 0]),
    ([0, 47, 390, 557], [47, 0, 34, 0], [0, 0, 0, 0]),
    ([20, 10, 370, 830], [47, 30, 34, 44], [37, 10, 20, 24]),
])
def test_the_safe_area_only_pads_hardware_that_intersects_the_canvas(rect, insets, expected):
    driver = """
const fs = require('fs'), vm = require('vm');
const [left,top,right,bottom] = JSON.parse(process.argv[1]), raw=JSON.parse(process.argv[2]);
const library={}, heap=new Float32Array(4);
const context={LibraryManager:{library}, mergeInto:Object.assign, HEAPF32:heap,
  Module:{canvas:{getBoundingClientRect:()=>({left,top,right,bottom})}},
  window:{innerWidth:390,innerHeight:844}, document:{createElement:()=>({style:{}}),body:{appendChild(){},removeChild(){}}},
  getComputedStyle:()=>({paddingTop:String(raw[0]),paddingRight:String(raw[1]),paddingBottom:String(raw[2]),paddingLeft:String(raw[3])})};
vm.runInNewContext(fs.readFileSync(process.argv[3],'utf8'),context);
library.StatecraftSafeAreaInsets(0);
console.log(JSON.stringify([...heap]));
"""
    result = subprocess.run(["node", "-e", driver, json.dumps(rect), json.dumps(insets), str(SOURCE)],
                            text=True, capture_output=True, check=True)
    assert json.loads(result.stdout) == expected


@pytest.mark.skipif(shutil.which("node") is None, reason="needs node for the shipped .jslib")
@pytest.mark.parametrize("top", [47, -33])
def test_the_template_owns_safe_edges_even_after_its_game_region_scrolls(top):
    driver = """
const fs=require('fs'), vm=require('vm'), library={}, heap=new Float32Array(4);
const canvas={getBoundingClientRect:()=>({left:0,top:Number(process.argv[1]),right:390,bottom:552})};
const context={LibraryManager:{library}, mergeInto:Object.assign, HEAPF32:heap, Module:{canvas},
  window:{innerWidth:390,innerHeight:844},
  document:{getElementById:()=>({contains:node=>node===canvas}),createElement:()=>({style:{}}),
    body:{appendChild(){},removeChild(){}}},
  getComputedStyle:()=>({paddingTop:'47',paddingRight:'0',paddingBottom:'34',paddingLeft:'0'})};
vm.runInNewContext(fs.readFileSync(process.argv[2],'utf8'),context);
library.StatecraftSafeAreaInsets(0);
console.log(JSON.stringify([...heap]));
"""
    result = subprocess.run(["node", "-e", driver, "--", str(top), str(SOURCE)],
                            text=True, capture_output=True, check=True)
    assert json.loads(result.stdout) == [0, 0, 0, 0]
