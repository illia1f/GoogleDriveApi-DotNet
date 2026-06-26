# Folder Hierarchy Internals

Implementation notes for traversing Google Drive's folder structure safely. User-facing
behavior lives in [guides/folders-and-hierarchy.md](../guides/folders-and-hierarchy.md);
this page is about _how_ to traverse without breaking.

## Why traversal needs care

Drive folders can have multiple parents (see the user guide), so the structure is a directed
graph, not a tree. A naive depth-first walk can revisit nodes or loop forever when a cycle
exists.

## Handling cycle dependencies

1. **Track visited nodes.** Maintain a set of visited ids during traversal. If a node is
   revisited, a cycle is detected — stop descending that branch.
2. **Limit recursion depth.** Enforce a maximum depth as a backstop against unexpected loops.
3. **Cycle-detection algorithms.** Use Depth-First Search (DFS) with explicit cycle detection
   to walk the graph safely.

Together these let you reconstruct a usable hierarchy from the flat list returned by
`Folders.ListAllAsync` without infinite loops.

> A concrete implementation is in the
> [RetrieveAllFolderHierarchy sample](https://github.com/Illia1F/GoogleDriveApi-DotNet/tree/main/samples/RetrieveAllFolderHierarchy).
