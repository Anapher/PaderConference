# Advantages over BigBlueButton
Initially, I created Strive because I was tired of BigBlueButton. I identified some points that I would do different than the creators of BBB. In this document, I want to describe advantages of Strive in comparison to BBB. Please note that I was a tutor at my university of a class of 20-30 students.

### Global chat
If breakout rooms are active, the global chat is very nice for students to ask questions, as all students no matter in which breakout room they currently are can see them (and especially the answer).

### Changable breakout room settings
A common use case I identified is that I want to extend the time the breakout rooms are active (or reduce it). In BBB, you have to recreate all breakout rooms in order to so which is a unnecessary interruption for the students.

### Announcements
Oftentimes, you want to announce something to all students in the breakout rooms, for examples instructions for a task they are currently working on.
In Strive, you can easily send a chat message as announcement which will trigger a huge overlay and a sound for all participants so it can't be missed.

### Audio switching
I have no idea who thought that would be a good idea, but in BBB you have to choice at the beginning whether you want to join with your microphone or not.
Bonus point is that you always have to execute a sound test which takes some seconds. You can only change that by completely disconnecting from the audio and reconnecting (including the audio test).
For breakout rooms, you have to again completely disconnect from the main conference. In Strive, we don't do that. You are automatically connected to listen
and you can always enable your microphone if you have the permissions.
Breakout room switching won't disconnect you from the conference, you don't have to completely reconnect. Also, you are not forced to do a microphone test every single time you want to join a room.

### Use of space
BBB wastes a lot of space that could be used for content. In Strive, I try to leave as little blank space as possible.

### Media transmission
In BBB, webcams send a single webcam stream with 360p, no matter how large the video is displayed. If a lot of participants are joined, this drastically reduces performance as every
webcam stream has a much higher quality than needed. In contrast, if the webcam video is enlarged, the quality is far too low.

Strive uses [simulcast](https://en.wikipedia.org/wiki/Simulcast) which means that every webcam stream is available in multiple resolutions (180p, 360p, 720p)
and the best fitting one is selected. If the network speed is low, a lower resolution is automatically selected. This contributes to a much better performance for conferences with a large amount of participants.

Also, the creators of [mediasoup](https://mediasoup.org/) (the SFU for WebRTC) are doing a great job optimizing the transmission of streams,
here you can [find a comparison](https://webrtchacks.com/sfu-load-testing/). I fucking love mediasoup!

### Anonymous chat messages
A huge problem I faced in the class I supervised were very few asked questions by the participants, not because they all were highly intelligent but they did not dare to ask them.
In Strive, it is possible to permit all participants to send chat messages anonymously. This is really nice for asking trivial questions, as the questioner can stay anonymous.
Of course this feature can be disabled at any time to counter misuse but I think it's worth a try.

### Load balancing
Strive is load balancable by design, meaning you can scale the application over as many servers as you want and it will just work.

### Equipment
Everyone has a smartphone with a good camera but not everyone has a webcam. In Strive, you can add your devices by scanning a qr code and access the microphone, camera or screen - without rejoining with a second account.

### Volume of participants can be adjusted individually
Every participant has a volume slider that can be adjusted. Also, you can adjust the volume of your current microphone in a range of 0% to 200%.

### Polls
While I hold my class in the Corona crisis, polls were a really nice feature to interact with my students. Sadly, BBB has a very limited implementation of polls. In Strive, you can choose between single choice, multiple choice, numeric and tag cloud polls (BBB offers single choice only). Furthermore you can decide for each poll whether it is anonymous, if it's global (for all rooms or just the current one) and you can select whether participants can change their answers after submission.

A nice use case with the polls in Strive is to evaluate the current status of students that work on a task. You can create a global, anonymous, answer-non-final poll with the options "finished", "surrendered", "need time" (also available as template) so participants can select their current status and even change it afterwards.

## Missing features in Strive
To be fair, I want to list the features of BBB that come to my mind that are still missing in Strive.

### Recording conferences
I'm not sure if I actually want to solve this with Strive. Recording would be a very huge feature that requires a lot of infrastructure. Strive was created for casual class conferences and I don't really know of these need recording. Also, there exist great tools like Open Broadcaster that can be used.

### PDF upload / whiteboard
BBB supports uploading PDF files that can be shown in the conference. You can draw on these PDFs to explain topics. To be fair, the whiteboard of BBB is not the best compared to PowerPoint or other tools and I don't know if I could actually match the whiteboard of these professional programs. In my opinion, it would anyways be much easier to just share your screen and use one of these tools.
