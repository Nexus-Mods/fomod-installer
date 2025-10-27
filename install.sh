#!/bin/bash
# Installation script for meta package on Unix/Linux/macOS
# Sets FOMOD_METAPACKAGE_INSTALL=1 to skip child package builds

echo "Installing meta package with FOMOD_METAPACKAGE_INSTALL=1"
echo "Child packages will skip their build steps"
echo ""

FOMOD_METAPACKAGE_INSTALL=1 yarn install

if [ $? -eq 0 ]; then
    echo ""
    echo "Meta package installation complete!"
    echo "The sub-packages are linked but not built."
else
    echo ""
    echo "Meta package installation failed!"
    exit $?
fi
