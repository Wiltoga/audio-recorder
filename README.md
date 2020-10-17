\<icon\>
# Audio Recorder

Audio Recorder is a recording app that keeps only the few last seconds and can export them to audio file to sample stuff.

## Features

- Select which device to capture
- Can define how much time to record (30 seconds by default)
- Output file can be auto generated
- You can close the cmd/UI, the recorder will still continue in background
- Full command line support
- Full UI support
- You can stop at any moment without the need to save (the samples are saved in the RAM)

---

### Command line :

To see the available devices, use :
```
audiorec view devices all
```
The given devices follow this pattern : `<id of the device>|<name of the device>|<byte rate>`
Then select the ones you want to record :
```
audiorec record <the name, or the id of the first device> [<the name, or the id of the second device>] ...
```
Then once you have recorded the stuff you want, output it :
```
audiorec out <path.wav>
```
Or to let the app generate the name file :
```
audiorec out auto <folder path>
```
Note : when using `out`, it does not stop the recording, it just saves it. To stop recording, use :
```
audiorec stoprecord
```
Or to stop the entire recorder instance :
```
audiorec stop
```
More commands available in the help panel (call `audiorec` without params):
```
audiorec
```

### User interface :

TODO