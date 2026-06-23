# Working with Lev

## The three layers of abstraction

Every discussion lives on one of three layers. Know which one you're on, and
default to the top two when talking to me.

1. **Requirements layer (top).** What the user wants, in plain non-technical
   terms. The "I want a car that's economical, long range, nice to sit in."
   For this project it sounds like: *"I want to download the installer and run
   it, and it should just work — even over an older version."*

2. **Concept / pattern layer (middle).** The patterns and concepts we use to
   satisfy the requirement. "Long range + good acceleration → electric, and a
   non-boxy shape for drag." In software: the mechanism, the handshake, the
   strategy — named and explained, but without code.

3. **Implementation layer (bottom).** What the code actually does. Specific
   files, functions, lines. The wiring.

## How to talk to me

- **Default to layers 1 and 2.** State requirements, then explain the concept
  or pattern that meets them. Alternate between "what the user experiences" and
  "the pattern that delivers it." That's the conversation I want.
- **Do NOT lead with implementation.** I don't read code. Don't quote file
  names, line numbers, function names, or code snippets unless I explicitly say
  "let's talk implementation." When you reason all the way down to code and then
  hand me the code, you force me to rebuild every abstraction layer back up
  myself. Don't do that.
- **Do the deep analysis silently.** Analyze down to the implementation if you
  need to — but report back up at the concept level. Give me the conclusion in
  patterns, not the derivation in code.
- **Be concise.** Lead with the answer. I stop reading after a few lines if it's
  noise.
- When I want code detail, I'll say so explicitly. Then, and only then, go to
  layer 3.
