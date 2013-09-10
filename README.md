HaywireOwin
===========

Owin (http://owin.org/) implementation on top of the Haywire HTTP server (https://github.com/kellabyte/Haywire)


Currently very expermental.
Just proving technologies (in the C# side) at this stage 


###Project Goals:

#####Standards:
Implement the full Owin 1.0 specification.
Pass all Owin 1.0 specification unit test

#####Performance:
Will only add a 20% overhead above the haywire benchmarks

#####Cross platform:
Will work cross platform (*nix win osx)


#####Simple to use:
Painless deployment / installation etc
Can hotswap application Dlls (seamless switchover)
Web based admin console


##Current design/architecture/idea (Heavily subject to change)

Building in C# as a starting point and the porting to C once I have that working.

Workflow:

Haywire puts a http request into a queue
A thread takes items from this queue and writes a 'process this for me OWIN' event to a Memory Mapped File.
The event includes a pseudo (offset) pointer and length to where the 'native' haywire request has been places on a section of the memory mapped file.
 
On the C# side another thread looks for these events.
After reading it, it ACKs (letting the C thread write another event to that slot) and asynchronously passes the event to the OWIN processing function

It (will) pass the OWIN interface an object that implements IDictionary. 
The dictionary values will be mapped back to the 'native' haywire request.
(This is to avoid memory copying and Serialization /translation over heads)

When the OWIN Interface consumer returns the request/response

The process is then reversed passing the object -> event  -> memory mapped bridge -> haywire -> HTTP Response



Well that is the intent much still to be implemented.


Thoughts, suggestions and criticisms’ welcome.
