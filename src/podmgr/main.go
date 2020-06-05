package main

import (
	"flag"
	"fmt"
	"os"
)

// https://stackoverflow.com/questions/48366173/how-do-i-build-a-static-go-binary-for-the-docker-alpine-image
// https://rollout.io/blog/building-minimal-docker-containers-for-go-applications/

func help() {
	fmt.Println("usage: podmgr [--pod <podName>] <verb> ... ")
	fmt.Println("\t\t(pod defaults to pod job is run in. override for interactive testing)")
	fmt.Println("")
	fmt.Println("\tverbs:")
	fmt.Println("\t\tadd job-container <imageName>")
	fmt.Println("\t\trm job-container <imageName>")
	fmt.Println("\t\tadd debug-container <imageName>")
	fmt.Println("\t\trm debug-container <imageName>")
	fmt.Println("")
}

func main() {
	var pod string
	flag.StringVar(&pod, "pod", "", "optional pod name")

	showHelp := flag.Bool("help", false, "show help")
	flag.Parse()

	if *showHelp {
		help()
		os.Exit(0)
	}

	args := flag.Args()
	if len(args) < 3 {
		help()
		os.Exit(1)
	}

	fmt.Printf("%v", args)
	verb := args[0]
	noun := args[1]
	data := args[2:]

	if len(pod) == 0 {
		pod = os.Getenv("RUNNER_POD_NAME")
	}
	if len(pod) == 0 {
		fmt.Println("Pod name not detected.  If interactive, supply with --pod")
		os.Exit(1)
	}

	fmt.Printf("%v %v %v %v\n", pod, verb, noun, data)

	action := fmt.Sprintf("%v:%v", verb, noun)
	fmt.Printf("%v\n", action)

	var err error
	switch action {
	case "add:job-container":
		err = ensureJobContainer(pod, data[0])
		break
	case "add:debug-container":
		err = ensureDebugContainer(pod, data[0])
		break
	case "rm:job-container":
		err = rmJobContainer(pod, data[0])
		break
	default:
		help()
		os.Exit(1)
	}

	if err != nil {
		fmt.Printf("Failed: %v\n", err)
		os.Exit(1)
	}

	// v1Client, err := getCoreV1()
	// if err != nil {
	// 	panic(err)
	// }

	// pods, _ := v1Client.Pods("").List(v1.ListOptions{})

	// //fmt.Printf("There are %v %d pods in the cluster\n", pod.Name, len(pods.Items))

	// for i, pod := range pods.Items {
	// 	fmt.Printf("Pod %d, %v\n", i, pod.Name)
	// }
}
