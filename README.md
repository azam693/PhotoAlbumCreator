PhotoAlbumGen — a console utility for creating standalone photo albums.

# Purpose

Creates a shared `System` folder with settings and scripts.  
Creates new albums: `<AlbumName>/Files/` + `index.html`.  
Fills `index.html` with photo/video cards from the `Files` folder.

# Usage

Run PhotoAlbumGen with one of the following commands:
- help:
  - Show brief help.
- init [--force]:
  - Create/update the `System/` folder from embedded resources;
  - Without `--force` files are not overwritten; with `--force` they are overwritten;
- new:
  - Create a new album (will prompt for a name);
    - Creates the `<Album>/Files/` folder and an `index.html` file with basic markup;
  - Automatically ensures the `System` folder exists;
- fill:
  - Before running, put photo and video files into the `Files` folder;
  - Scans `<Album>/Files/`, sorts files by date (earliest → latest);
  - While building the album, photo files are grouped by creation time;
  - Automatically (re)creates the `System` folder and the photo album if they don’t exist;
  - Sets the publish date to the earliest modification date (format is taken from settings);
  - Inserts commented text blocks between media blocks;
    - Uncomment these blocks to add descriptions to photos/videos;

# Project/album structure

- System/
  - styles.css;
  - script.js;
  - appsettings.json (settings; if absent, an embedded default is used);

- `<Album>`/
  - Files/ (place your photos/videos here);
  - index.html;

# Supported file formats

- Photos: .jpg .jpeg .png .gif .webp .bmp .tiff
- Videos: .mp4 .webm .mov .m4v .avi .mkv

# Notes

- All generated files use UTF-8 encoding; `index.html` contains `<meta charset="utf-8">`;
- For development use Debug (F5). Native AOT is for Release publishing only;
