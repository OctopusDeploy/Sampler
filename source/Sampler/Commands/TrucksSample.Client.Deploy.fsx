let trackerUrl = Octopus.tryFindVariable "TrackerUrl"
match trackerUrl with
    | Some x -> printf "Tracker URL: %s" x
    | None -> printf "Tracker URL not found"