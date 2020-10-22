# files-as-a-service
To provide a file bucketed file system abstracted from any storage mechanisms.

## Features
* Versioned files.
* File meta data and file data separated.

## TODO
* Add messaging bus for scheduled tasks and executing actions off the call.
* Add ACL (Don't know if I want per file for per bucket ACL).
* Add API layer.
* Add UI.

## Notes
* Adding the message bug ran into problems without a central point to access entities. Had to add a bunch of plumbing to go through the container to delete a version from the store. Need direct access to the store.