#!/bin/bash
tr -d '\r' < build.sh > build.fix.sh
rm build.sh
mv build.fix.sh build.sh
