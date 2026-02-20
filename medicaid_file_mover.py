#!/usr/bin/env python3
"""
Script to find XML files containing "Medicaid" (case insensitive) and move them to a "Medicaid" subfolder.
"""

import os
import shutil
import xml.etree.ElementTree as ET
from pathlib import Path
import argparse


def contains_medicaid(file_path):
    """
    Check if an XML file contains the text "Medicaid" (case insensitive).
    
    Args:
        file_path (str): Path to the XML file
        
    Returns:
        bool: True if "Medicaid" is found, False otherwise
    """
    try:
        # Parse the XML file
        tree = ET.parse(file_path)
        root = tree.getroot()
        
        # Convert the entire XML tree to string and search for "Medicaid"
        xml_text = ET.tostring(root, encoding='unicode', method='text')
        
        # Case insensitive search
        return "medicaid" in xml_text.lower()
        
    except ET.ParseError:
        print(f"Warning: Could not parse XML file: {file_path}")
        return False
    except Exception as e:
        print(f"Error reading file {file_path}: {e}")
        return False


def move_xml_files_with_medicaid(folder_path, dry_run=False):
    """
    Find XML files containing "Medicaid" and move them to a "Medicaid" subfolder.
    
    Args:
        folder_path (str): Path to the folder containing XML files
        dry_run (bool): If True, only show what would be moved without actually moving files
    """
    folder_path = Path(folder_path)
    
    if not folder_path.exists():
        print(f"Error: Folder '{folder_path}' does not exist.")
        return
    
    if not folder_path.is_dir():
        print(f"Error: '{folder_path}' is not a directory.")
        return
    
    # Create Medicaid subfolder if it doesn't exist
    medicaid_folder = folder_path / "Medicaid"
    
    if not dry_run and not medicaid_folder.exists():
        medicaid_folder.mkdir()
        print(f"Created folder: {medicaid_folder}")
    
    # Find all XML files in the folder
    xml_files = list(folder_path.glob("*.xml"))
    
    if not xml_files:
        print(f"No XML files found in '{folder_path}'.")
        return
    
    print(f"Found {len(xml_files)} XML files to check...")
    
    moved_count = 0
    
    for xml_file in xml_files:
        if contains_medicaid(xml_file):
            destination = medicaid_folder / xml_file.name
            
            if dry_run:
                print(f"Would move: {xml_file.name} -> Medicaid/{xml_file.name}")
            else:
                try:
                    shutil.move(str(xml_file), str(destination))
                    print(f"Moved: {xml_file.name} -> Medicaid/{xml_file.name}")
                    moved_count += 1
                except Exception as e:
                    print(f"Error moving {xml_file.name}: {e}")
        else:
            if dry_run:
                print(f"Would keep: {xml_file.name} (no Medicaid found)")
    
    if not dry_run:
        print(f"\nMoved {moved_count} files to the Medicaid folder.")
    else:
        print(f"\nDry run complete. Would move files containing 'Medicaid' to Medicaid folder.")


def main():
    parser = argparse.ArgumentParser(
        description="Find XML files containing 'Medicaid' and move them to a Medicaid subfolder."
    )
    parser.add_argument(
        "folder_path",
        help="Path to the folder containing XML files"
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Show what would be moved without actually moving files"
    )
    
    args = parser.parse_args()
    
    print(f"Searching for XML files containing 'Medicaid' in: {args.folder_path}")
    print(f"Mode: {'Dry run' if args.dry_run else 'Live run'}")
    print("-" * 50)
    
    move_xml_files_with_medicaid(args.folder_path, dry_run=args.dry_run)


if __name__ == "__main__":
    main()

