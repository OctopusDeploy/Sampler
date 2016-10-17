let conn = Octopus.tryFindVariable "DatabaseConnectionString"
match conn with
    | Some x -> printf "Database: %s" x
    | None -> printf "DatabaseConnectionString not found"