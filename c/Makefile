CC = gcc
CCFLAGS = -Wall

SRCDIR = src
BINDIR = bin

SRCFILES = $(wildcard $(SRCDIR)/*.c)
OBJFILES = $(SRCFILES:$(SRCDIR)/%.c=$(BINDIR)/%.o)
EXEC = $(BINDIR)/socket

all: $(EXEC)

release: CCFLAGS += -O2
release: $(EXEC)

debug: CCFLAGS += -O0 -g
debug: $(EXEC)

clean:
	rm $(BINDIR)/*.o
	rm $(EXEC)

$(EXEC): $(OBJFILES)
	$(CC) $(CCFLAGS) $(OBJFILES) -o $(EXEC)

$(BINDIR)/%.o: $(SRCDIR)/%.c
	$(CC) $(CCFLAGS) -c $< -o $@

.phony: all release debug clean