# Quay Code

<img width="960" alt="snap2023-12-23-15-06-59 2" src="https://github.com/Dormarr/Quay-Code/assets/134154948/63dddad1-51a4-407a-bd1a-c6f7d7945660">


Quay Code is a desktop based, open source CMYK 2D matrix code generator and reader. Currently it only runs on Windows but macOS and iOS development is underway. It uses dual binary encoding, meaning each colour contains 2 binary bits, allowing for potentially twice the data to be stored in each code. It uses Reed Solomon Error Correction for reliable decoding, and I'm constantly working on enhancing the CV capabilities for better and faster recognition of the codes.

It's still in very early development, but the foundations are there.

---
<h2>Technology</h2>
- C#
- .NET Stack/.NET Core 6.0
- WPF
- Visual Studio
- Git
- Original UX Design

---
<h2>Motivation</h2>
The idea began from questioning why there are no triangular QR codes, which fell into questioning the limited customisation of QR codes in general. The more I researched the more fun it seemed to create my own from scratch. I want to prove to myself that I was able to write the code from scratch, being a UX designer by trade and amateur developer by hobby. Though the triangular QR code has not yet come to fruition, the Quay Code is a project I am incredibly proud of and continue to develop and explore the use cases of.

Although I fiercely admire the global compatibility and ISO standards of QR codes, I believe we should have more options, more freedom and independence and variation in the technology we use. There is value to centralisation, but also to customisation.

---
<h2>The Nitty Gritty</h2>
<h3>Generating</h3>

This is my first full scale programming project, so I was kind of making it up as I went along on nothing but faith, coffee and StackOverflow threads. But the starting line was simply encoding data. I realised from the start that a CMYK colour scheme would allow for binary pairs, so I began working out how to convert a string to binary which I could then split into pairs and associate with a distinct colour seeing as there are only 4 binary pair combinations.

Once I had the binary sorted, I wanted to focus on error correction codes. QR codes always amazed me that they could be partially covered and still readable, in all honesty it still amazes me. I dived deep into polynomials, coefficients and Euclidian mathematics but my brain hurt so I ended up using a library.

The user inputs the text they're using for their code, the text is padded to next limit of the code size, that being 12x12, 18x18 and 32x32 (excluding the border), and the padding amount is used for the correction code bits, with a minimum set so you can maximize error correction capabilities depending on message length.

Once the ECC is applied, it converts to binary pairs and sends through to the graphic rendering stage. Here it measures the length of the input and thus the size of the code. The code is drawn as individual pixels and blown up to fit the pre-existing bitmap within the application, so the size of the code is vital to ensure it fits appropriately to the bitmap and renders as a full image for download.

I actually do not use raw CMYK values for the output code, rather slightly adjusted, softer colours for aesthetic value. I intend to stress test each version to determine how much of a performance sacrifice this is, but so far it has not shown to be of terrible consequence.

![Quay Templates](https://github.com/Dormarr/Quay-Code/assets/134154948/1ba86db6-30e0-42fb-94ac-e92969b288de)

Early in the project I drew out the code templates, with the data slots clearly visible. I went through multiple iterations and intend to do further testing once I have a fully stable build of the app, but the current format works. The code divides the bitmap up into a pixel coordinate grid and I use these coordinates as slots for the bitmap rendering. I have constant data slots for each size, and for black, white, and CMYK values. Black and White are used for the template and border, so remain constant, and the data slots vary obviously due to the input of data but use the full CMYK palette. The data coordinate info also comes in handy for the reading stage as it limits the analysis to the appropriate slots.

I spent a lot of time trying to work out the best way to define different variable settings within the code by using a header/prefix in designated slots. This would include any masking I wanted to implement down the line to break up monotony, but currently I only use it to identify the length of the message so that, when the code is read, the output will be trimmed to the appropriate character length.

Currently I have 6 slots in Q12 and Q18 and 16 slots in Q32 assigned specifically to act as a prefix, most being currently unused. There are also 5 CMYK and white control slots to ensure the colours are produced properly.

<h3>Reading</h3>
The reading of the code was a completely new challenge to me and, as it currently stands, is not very good. I use the EMGU library for computer vision and have not spent an awful lot of time tinkering with it.

Currently, it identifies the Quay in an image input by locating squares in the image, taking a temporary image, straightening it and identifying the black and white structure of the top of the code itself (excluding the 2 square width border). This is not an ideal way to do it but again, I haven't spent enough time on it.

Once it has located a code, it identifies the size based on the combination of white and black squares and then break it down into a coordinate grid, where it uses the same data coordinates as used to write the code to read it. It increases the saturation and other image adjustments to maximise the difference in hue for each square, then takes a pixel from the middle of each slot and determines which colour that corresponds to. It matches the binary pairings, joins into a full binary string, converts it back to text and runs it through error correction before displaying an output.

---
<h2>Issues & Improvements</h2>

**Data Masking**
I want to implement data masking as used in standard QR codes to break up repeated colours in the code. Lumps of the same colour can skew the output, but applying one of multiple masks can avoid this. I would need to create a way to analyse the code after generation but before rendering to ensure the most effective output is rendered. I could also use some of the prefix slots to define which mask would be used.

**Better Reading**
All around, this is one of the bigger problems. CV is a tough thing to wrap your head around so this will certainly take some work but I want to use more effective tracking of the codes using the tracking square present in each size format, as well as using multiple pixel samples from each coordinate to ensure data integrity, as this has been a problem. I also plan to sample the control colours on the code to ensure readability despite ambient colour distortion.

**Reliable Output**
Currently, the output is superimposed onto the webcam image of the code and it tends to flicker, misreading a character of two every few seconds depending on the environment. If I were to put a delay/buffer on the output, I could use the most common output for the code and disregard outliers. This would prevent the flickering of wrong output and dramatically increase output data integrity when eventually put to plain text within the application.

**Refactoring**
This always applies, but I essentially learned to code by doing this project, so a lot of the code is a complete mess. I plan to rewrite it from scratch and make necessary improvements, eliminate redundant packages etc.

**Others**
There are a bunch of bugs I've noticed and haven't had the time to squash, and plenty more I haven't noticed. If you happen across anything, please do flag it!

---
<h2>Planned Features</h2>
- QR Standard Read/Write alongside Quay.
- Desktop Snipping for code recognition.
- Add Settings
- Actionable Output.
- Finalised & Publicised Standards of Encoding/Decoding & Formatting.
- Android, iOS & macOS compatibility.
