# Stelle sicher, dass du im Git-Repository bist
git fetch origin

$main = "main"

# Alle lokalen Branches holen
$branches = git for-each-ref --format='%(refname:short)' refs/heads/

foreach ($branch in $branches) {
    $ahead  = git rev-list --count "$main..$branch"
    $behind = git rev-list --count "$branch..$main"

    Write-Output ("{0,-40} ahead {1,-4} behind {2,-4}" -f $branch, $ahead, $behind)
}
